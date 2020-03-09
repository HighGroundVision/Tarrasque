using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Exceptions;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.ProcessAbilities.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessAbilities.Services
{
    public interface IAbilityService
    {
        Task Process(Match item, IBinder binder, ILogger log);
        Task<List<DTO.AbilitySummary>> GetSummary(IBinder binder, ILogger log);
        Task<DTO.AbilityDetails> GetDetails(int id, IBinder binder, ILogger log);
        Task UpdateSummary(IBinder binder, ILogger log);
    }

    public class AbilityService : IAbilityService
    {
        private const string SUMMARY_PATH = "hgv-abilities/summary.json";
        private const string HISTORY_PATH = "hgv-abilities/{0}/history.json";
        private const string DETAILS_PATH = "hgv-abilities/{0}/details.json";
        private const string HISTORY_DELIMITER = "yy-MM-dd";

        private readonly MetaClient metaClient;

        public AbilityService(MetaClient metaClient)
        {
            this.metaClient = metaClient;
        }

        private static async Task<T> ReadData<T>(IBinder binder, BlobAttribute attr) where T : new()
        {
            var reader = await binder.BindAsync<TextReader>(attr);
            if (reader == null)
                throw new NotFoundException();

            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(input))
                throw new NotFoundException();

            return JsonConvert.DeserializeObject<T>(input);
        }

        private async Task ProcessBlob<T>(CloudBlockBlob blob, Action<T> updateFn, ILogger log) where T : new()
        {
            try
            {
                var exist = await blob.ExistsAsync();
                if (!exist)
                {
                    var data = new T();
                    var json = JsonConvert.SerializeObject(data);
                    await blob.UploadTextAsync(json);
                }
            }
            catch (Exception)
            {
            }

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync((n) => TimeSpan.FromMilliseconds(100));

            var ac = await policy.ExecuteAsync(async () =>
            {
                var leaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(30));
                if (string.IsNullOrEmpty(leaseId))
                    throw new NullReferenceException();

                return AccessCondition.GenerateLeaseCondition(leaseId);
            });

            try
            {
                var input = await blob.DownloadTextAsync(ac, null, null);
                var data = JsonConvert.DeserializeObject<T>(input);
                updateFn(data);
                var output = JsonConvert.SerializeObject(data);
                await blob.UploadTextAsync(output, ac, null, null);
            }
            finally
            {
                await blob.ReleaseLeaseAsync(ac, null, null);
            }
        }

        public async Task Process(Match match, IBinder binder, ILogger log)
        {
            var skills = this.metaClient.GetSkills();

            foreach (var player in match.players)
            {
                if (player.ability_upgrades == null)
                    continue;

                var abilities = player.ability_upgrades
                    .Select(_ => _.ability)
                    .Distinct()
                    .Join(skills, _ => _, _ => _.Id, (lhs, rhs) => rhs)
                    .ToList();

                foreach (var ability in abilities)
                {
                    await UpdateHistory(match, player, ability, binder, log);
                    await UpdateDetails(match, player, ability, abilities, binder, log);
                }
            }
        }

        private async Task UpdateHistory(Match match, Player player, Ability ability, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(HISTORY_PATH, ability.Id));
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            Action<AbilityHistory> updateFn = (history) =>
            {
                var victory = match.Victory(player);
                var ancestry = (ability.HeroId == player.hero_id);
                var priority = player.PickPriority();
                var date = DateTimeOffset.FromUnixTimeSeconds(match.start_time);
                var bounds = date.AddDays(-6);
                var mid = date.AddDays(-3);

                history.Total.Picks++;
                history.Total.Wins += victory ? 1 : 0;
                history.Total.Ancestry += ancestry ? 1 : 0;
                history.Total.Priority += priority;

                history.Data.Add(new AbilityHistoryVictory() { Timestamp = date, Victory = victory, Ancestry = ancestry, Priority = priority });
                history.Data.RemoveAll(_ => _.Timestamp < bounds);

                var current = history.Data.Where(_ => _.Timestamp > mid).ToList();
                history.Current.Picks = current.Count();
                history.Current.Wins = current.Count(_ => _.Victory == true);
                history.Current.Ancestry = current.Count(_ => _.Ancestry == true);
                history.Current.Priority = current.Sum(_ => _.Priority);

                var previous = history.Data.Where(_ => _.Timestamp < mid).ToList();
                history.Previous.Picks = previous.Count;
                history.Previous.Wins = previous.Count(_ => _.Victory == true);
                history.Previous.Ancestry = previous.Count(_ => _.Ancestry == true);
                history.Previous.Priority = previous.Sum(_ => _.Priority);
            };
            await ProcessBlob(blob, updateFn, log);
        }

        private async Task UpdateDetails(Match match, Player player, Ability ability, List<Ability> abilities, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(DETAILS_PATH, ability.Id));
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            Action<AbilityCombo> updateFn = (details) =>
            {
                var victory = match.Victory(player);

                // Hero Combo
                {
                    var item = details.Heroes.Find(_ => _.Id == player.hero_id);
                    if (item == null)
                    {
                        item = new AbilityComboStat() { Id = player.hero_id };
                        details.Heroes.Add(item);
                    }

                    item.Update(victory);
                }

                // Ability Combos
                foreach (var a in abilities)
                {
                    if (a.Id == ability.Id)
                        continue;

                    var item = details.Abilities.Find(_ => _.Id == a.Id);
                    if (item == null)
                    {
                        item = new AbilityComboStat() { Id = a.Id };
                        details.Abilities.Add(item);
                    }

                    item.Update(victory);
                }
            };
            await ProcessBlob(blob, updateFn, log);
        }

        public async Task UpdateSummary(IBinder binder, ILogger log)
        {
            var blob = await binder.BindAsync<CloudBlockBlob>(new BlobAttribute(SUMMARY_PATH));

            var data = new Dictionary<int, AbilityHistoryData>();
            var abilities = this.metaClient.GetSkills().Where(_ => _.AbilityDraftEnabled).ToList();
            foreach (var ability in abilities)
            {
                try
                {
                    var attr = new BlobAttribute(string.Format(HISTORY_PATH, ability.Id));
                    var history = await ReadData<AbilityHistory>(binder, attr);
                    data.Add(ability.Id, history);
                }
                catch (Exception)
                {
                }
            }

            Action<AbilitySummary> updateFn = (summary) => summary.Data = data;
            await ProcessBlob(blob, updateFn, log);
        }

        public async Task<List<DTO.AbilitySummary>> GetSummary(IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(SUMMARY_PATH));
            var summary = await ReadData<AbilitySummary>(binder, attr);

            var collection = new List<DTO.AbilitySummary>();
            var abilities = this.metaClient.GetSkills().Where(_ => _.AbilityDraftEnabled);
            foreach (var ability in abilities)
            {
                if (summary.Data.ContainsKey(ability.Id) == false)
                    continue;

                var data = summary.Data[ability.Id];

                var item = new DTO.AbilitySummary()
                {
                    Id = ability.Id,
                    Name = ability.Name,
                    Image = ability.Image,
                    Total = new DTO.AbilitySummaryHistory()
                    {
                        Ancestry = data.Total.Ancestry,
                        Picks = data.Total.Picks,
                        Priority = data.Total.Priority,
                        Wins = data.Total.Wins,
                    },
                    Current = new DTO.AbilitySummaryHistory()
                    {
                        Ancestry = data.Current.Ancestry,
                        Picks = data.Current.Picks,
                        Priority = data.Current.Priority,
                        Wins = data.Current.Wins,
                    },
                    Previous = new DTO.AbilitySummaryHistory()
                    {
                        Ancestry = data.Current.Ancestry,
                        Picks = data.Current.Picks,
                        Priority = data.Current.Priority,
                        Wins = data.Current.Wins,
                    },
                };
                collection.Add(item);
            }

            return collection;
        }

        public async Task<DTO.AbilityDetails> GetDetails(int id, IBinder binder, ILogger log)
        {
            var heroes = metaClient.GetHeroes();
            var skills = metaClient.GetSkills();
            var ability = skills.Find(_ => _.Id == id);
            if (ability == null)
                throw new ArgumentOutOfRangeException(nameof(id));

            var summary = await ReadData<AbilityHistory>(binder, new BlobAttribute(string.Format(HISTORY_PATH, id)));
            var combos = await ReadData<AbilityCombo>(binder, new BlobAttribute(string.Format(DETAILS_PATH, id)));

            var details = new DTO.AbilityDetails();
            details.Id = ability.Id;
            details.Name = ability.Name;
            details.Image = ability.Image;
            details.OrginalHeroId = ability.HeroId;

            details.Picks = summary.Total.Picks;
            details.Wins = summary.Total.Wins;
            details.History = summary.Data
                .GroupBy(_ => _.Timestamp.ToString(HISTORY_DELIMITER))
                .Select(_ => new DTO.AbilityDetailHistory()
                {
                    Day = _.Key,
                    Picks = _.Count(),
                    Wins = _.Count(_ => _.Victory)
                })
                .ToList();

            details.Heroes = combos.Heroes
               .Join(heroes, _ => _.Id, _ => _.Id, (lhs, rhs) => new DTO.AbilityDetailCombo()
               {
                   Id = rhs.Id,
                   Name = rhs.Name,
                   Image = rhs.ImageBanner,
                   Picks = lhs.Picks,
                   Wins = lhs.Wins,
               })
               .OrderByDescending(_ => _.WinRate)
               .ToList();

            details.Abilities = combos.Abilities
               .Join(skills, _ => _.Id, _ => _.Id, (lhs, rhs) => new DTO.AbilityDetailCombo()
               {
                   Id = rhs.Id,
                   Name = rhs.Name,
                   Image = rhs.Image,
                   Picks = lhs.Picks,
                   Wins = lhs.Wins,
               })
               .OrderByDescending(_ => _.WinRate)
               .ToList();

            return details;
        }
    }
}
