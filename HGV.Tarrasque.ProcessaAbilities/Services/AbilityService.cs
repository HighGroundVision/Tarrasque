using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.ProcessaAbilities.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessaAbilities.Services
{
    public interface IAbilityService
    {
        Task Process(Match item, IBinder binder, ILogger log);
        Task<List<AbilitySummaryStat>> GetSummary(IBinder binder, ILogger log);
        Task<AbilityDetails> GetDetails(int id, IBinder binder, ILogger log);
    }

    public class AbilityService : IAbilityService
    {
        private const string SUMMARY_PATH = "hgv-abilities/{0}/summary.json";
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

            T data;
            if (reader == null)
            {
                data = new T();
            }
            else
            {
                var input = await reader.ReadToEndAsync();
                data = JsonConvert.DeserializeObject<T>(input);
            }

            return data;
        }

        private static async Task WriteData<T>(IBinder binder, BlobAttribute attr, T data)
        {
            var output = JsonConvert.SerializeObject(data);
            var writer = await binder.BindAsync<TextWriter>(attr);
            await writer.WriteAsync(output);
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
                    await UpdateSummary(match, player, ability, binder, log);
                    await UpdateDetails(match, player, ability, abilities, binder, log);
                }
            }
        }

        private static async Task UpdateSummary(Match match, Player player, Ability ability, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(SUMMARY_PATH, ability.Id));
            var summary = await ReadData<AbilitySummary>(binder, attr);

            var victory = match.Victory(player);
            var ancestry = (ability.HeroId == player.hero_id);
            var priority = player.PickPriority();
            var date = DateTimeOffset.FromUnixTimeSeconds(match.start_time);
            var bounds = date.AddDays(-6);
            var mid = date.AddDays(-3);

            summary.Total.Picks++;
            summary.Total.Wins += victory ? 1 : 0;
            summary.Total.Ancestry += ancestry ? 1 : 0;
            summary.Total.Priority += priority;

            summary.Data.Add(new AbilitySummaryVictory() { Timestamp = date, Victory = victory, Ancestry = ancestry, Priority = priority });
            summary.Data.RemoveAll(_ => _.Timestamp < bounds);

            var current = summary.Data.Where(_ => _.Timestamp > mid).ToList();
            summary.Current.Picks = current.Count();
            summary.Current.Wins = current.Count(_ => _.Victory == true);
            summary.Current.Ancestry = current.Count(_ => _.Ancestry == true);
            summary.Current.Priority = current.Sum(_ => _.Priority);

            var previous = summary.Data.Where(_ => _.Timestamp < mid).ToList();
            summary.Previous.Picks = previous.Count;
            summary.Previous.Wins = previous.Count(_ => _.Victory == true);
            summary.Previous.Ancestry = previous.Count(_ => _.Ancestry == true);
            summary.Previous.Priority = previous.Sum(_ => _.Priority);

            await WriteData(binder, attr, summary);
        }

        private static async Task UpdateDetails(Match match, Player player, Ability ability, List<Ability> abilities, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(DETAILS_PATH, ability.Id));
            var details = await ReadData<AbilityCombo>(binder, attr);

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

            await WriteData(binder, attr, details);
        }

        public async Task<List<AbilitySummaryStat>> GetSummary(IBinder binder, ILogger log)
        {
            var collection = new List<AbilitySummaryStat>();

            var heroes = this.metaClient.GetHeroes();
            foreach (var hero in heroes)
            {
                var attr = new BlobAttribute(string.Format(SUMMARY_PATH, hero.Id));
                var summary = await ReadData<AbilitySummaryStat>(binder, attr);
                collection.Add(summary);
            }

            return collection;
        }

        public async Task<AbilityDetails> GetDetails(int id, IBinder binder, ILogger log)
        {
            var heroes = metaClient.GetHeroes();
            var skills = metaClient.GetSkills();
            var ability = skills.Find(_ => _.Id == id);
            if (ability == null)
                throw new ArgumentOutOfRangeException(nameof(id));

            var summary = await ReadData<AbilitySummary>(binder, new BlobAttribute(string.Format(SUMMARY_PATH, id)));
            var combos = await ReadData<AbilityCombo>(binder, new BlobAttribute(string.Format(DETAILS_PATH, id)));

            var details = new AbilityDetails();
            details.Id = ability.Id;
            details.Name = ability.Name;
            details.Image = ability.Image;
            details.OrginalHeroId = ability.HeroId;

            details.Picks = summary.Total.Picks;
            details.Wins = summary.Total.Wins;
            details.History = summary.Data
                .GroupBy(_ => _.Timestamp.ToString(HISTORY_DELIMITER))
                .Select(_ => new AbilityDetailHistory()
                {
                    Day = _.Key,
                    Picks = _.Count(),
                    Wins = _.Count(_ => _.Victory)
                })
                .ToList();

            details.Heroes = combos.Heroes
               .Join(skills, _ => _.Id, _ => _.Id, (lhs, rhs) => new AbilityDetailCombo()
               {
                   Id = rhs.Id,
                   Name = rhs.Name,
                   Image = rhs.Image,
                   Picks = lhs.Picks,
                   Wins = lhs.Wins,
               })
               .OrderByDescending(_ => _.WinRate)
               .ToList();

            details.Abilities = combos.Abilities
               .Join(skills, _ => _.Id, _ => _.Id, (lhs, rhs) => new AbilityDetailCombo()
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
