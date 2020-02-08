using Dawn;
using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using HGV.Tarrasque.Common.Extensions;
using Newtonsoft.Json;
using HGV.Tarrasque.Common.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using Microsoft.WindowsAzure.Storage;
using Polly;
using System.Linq;
using System.Collections.Generic;

namespace HGV.Tarrasque.ProcessMatch.Services
{
    public interface IProcessMatchService
    {
        Task ProcessMatch(Match match, IBinder binder, ILogger log);
    }

    public class ProcessMatchService : IProcessMatchService
    {
        public async Task ProcessMatch(Match match, IBinder binder, ILogger log)
        {
            var start = DateTime.Now;

            //log.LogWarning($"FnTimer[Start]: {DateTime.Now - start}");

            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(binder, nameof(binder)).NotNull();
            Guard.Argument(log, nameof(log)).NotNull();

            var timestamp = match.GetDate();

            await UpdateRegionCount(timestamp, match, binder, log);

            //log.LogWarning($"FnTimer[Region]: {DateTime.Now - start}");

            foreach (var player in match.players)
            {
                var hero = player.GetHero();
                await UpdateHeroCounts(timestamp, match, player, hero, binder, log);

                //log.LogWarning($"FnTimer[Hero]: {DateTime.Now - start}");

                var skills = player.GetSkills();
                foreach (var ability in skills)
                {
                    await UpdateAbilityCounts(timestamp, match, player, ability, binder, log);

                    await UpdateHeroComboCounts(timestamp, match, player, hero, ability, binder, log);
                }

                //log.LogWarning($"FnTimer[Abilities]: {DateTime.Now - start}");

                var pairs = player.GetPairs();
                foreach (var pair in pairs)
                {
                    await UpdateAbilityComboCounts(timestamp, match, player, hero, pair.Item1, pair.Item2, binder, log);
                    await UpdateAbilityComboCounts(timestamp, match, player, hero, pair.Item2, pair.Item1, binder, log);
                }

                // log.LogWarning($"FnTimer[Combos]: {DateTime.Now - start}");

                var talents = player.GetTalenets();
                foreach (var talent in talents)
                {
                    await UpdateTalenetCounts(timestamp, match, player, talent, binder, log);
                }

                //log.LogWarning($"FnTimer[Talents]: {DateTime.Now - start}");
            }

            log.LogWarning($"FnTimer[Finsihed]: {DateTime.Now - start}");
        }

        private async Task UpdateRegionCount(string timestamp, Match match, IBinder binder, ILogger log)
        {
            var regionId = match.GetRegion();
            var attr = new BlobAttribute($"hgv-regions/{timestamp}/{regionId}.json");

            await UpdateModel<RegionModel>(attr, binder, log, _ => {
                _.Date = timestamp;
                _.Region = regionId;
                _.Total++;
            });
        }

        private async Task UpdateHeroCounts(string timestamp, Match match, Player player, Hero hero, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute($"hgv-heroes/{timestamp}/{hero.Id}.json");

            await UpdateModel<HeroModel>(attr, binder, log, _ => 
            {
                _.Date = timestamp;
                _.HeroId = hero.Id;
                _.HeroName = hero.Name;
                _.Total++;
                _.Wins += match.Victory(player) ? 1 : 0;
            });
        }

        private async Task UpdateAbilityCounts(string timestamp, Match match, Player player, Ability ability, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute($"hgv-abilities/{timestamp}/{ability.Id}.json");

            await UpdateModel<AbilityModel>(attr, binder, log, _ => 
            {
                _.Date = timestamp;
                _.AbilityId = ability.Id;
                _.AbilityName = ability.Name;
                _.Total++;
                _.Wins += match.Victory(player) ? 1 : 0;
                _.Priority += player.PickPriority();
                _.Ancestry += (player.hero_id == ability.HeroId) ? 1 : 0;
            });
        }

        private async Task UpdateHeroComboCounts(string timestamp, Match match, Player player, Hero hero, Ability ability, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute($"hgv-hero-combos/{timestamp}/{hero.Id}/{ability.Id}.json");

            await UpdateModel<HeroComboModel>(attr, binder, log, _ =>
            {
                _.Date = timestamp;
                _.HeroId = hero.Id;
                _.HeroName = hero.Name;
                _.AbilityId = ability.Id;
                _.AbilityName = ability.Name;
                _.Total++;
                _.Wins += match.Victory(player) ? 1 : 0;
            });
        }

        private async Task UpdateTalenetCounts(string timestamp, Match match, Player player, Talent talent, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute($"hgv-talenets/{timestamp}/{talent.Id}.json");

            await UpdateModel<TalentModel>(attr, binder, log, _ =>
            {
                _.Date = timestamp;
                _.TalentId = talent.Id;
                _.TalentName = talent.Name;
                _.Total++;
                _.Wins += match.Victory(player) ? 1 : 0;
            });
        }

        private async Task UpdateAbilityComboCounts(string timestamp, Match match, Player player, Hero hero, Ability primaryAbility, Ability comboAbility, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute($"hgv-ability-combos/{timestamp}/{primaryAbility.Id}/{comboAbility.Id}.json");

            await UpdateModel<AbilityComboModel>(attr, binder, log, _ =>
            {
                _.Date = timestamp;
                _.PrimaryAbilityId = primaryAbility.Id;
                _.PrimaryAbilityName = primaryAbility.Name;
                _.ComboAbilityId = comboAbility.Id;
                _.ComboAbilityName = comboAbility.Name;
                _.Total++;
                _.Wins += match.Victory(player) ? 1 : 0;
            });
        }

        private static async Task UpdateModel<T>(BlobAttribute attr, IBinder binder, ILogger log, Action<T> _update) where T : new()
        {
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            try
            {
                var exists = await blob.ExistsAsync();
                if (exists == false)
                {
                    var data = new T();
                    var output = JsonConvert.SerializeObject(data);
                    await blob.UploadTextAsync(output);
                }
            }
            catch(Exception ex)
            {
                log.LogWarning(ex.Message);
            }

            var leaseId = await AcquireLease(blob, log);
            var condition = AccessCondition.GenerateLeaseCondition(leaseId);
            try
            {
                var input = await blob.DownloadTextAsync(condition, null, null);
                var data = JsonConvert.DeserializeObject<T>(input);
                
                _update(data);

                var output = JsonConvert.SerializeObject(data);
                await blob.UploadTextAsync(output, condition, null, null);
            }
            finally
            {
                await blob.ReleaseLeaseAsync(condition);
            }
        }

        private static async Task<string> AcquireLease(CloudBlockBlob blob, ILogger log)
        {
            var policy = Policy
                 .Handle<Exception>()
                 .WaitAndRetryForeverAsync(
                    (times) => TimeSpan.FromSeconds(1), 
                    (ex, n, time) => log.LogError($"AcquiringLeaseFailed: {n}")
                );

            var leaseId = await policy.ExecuteAsync<string>(async () =>
            {
                var timeout = TimeSpan.FromSeconds(60);
                return await blob.AcquireLeaseAsync(timeout);
            });

            return leaseId;
        }
    }
}
