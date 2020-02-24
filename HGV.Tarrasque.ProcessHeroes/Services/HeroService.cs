using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.ProcessHeroes.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessHeroes.Services
{
    public interface IHeroService
    {
        Task Process(Match match, IBinder binder, ILogger log);
        Task<List<HeroSummaryDelta>> GetSummary(IBinder binder, ILogger log);
        Task<HeroDetail> GetDetails(int id, IBinder binder, ILogger log);
    }

    public class HeroService : IHeroService
    {
        private const string SUMMARY_PATH = "hgv-heroes/{0}/summary.json";
        private const string DETAILS_PATH = "hgv-heroes/{0}/details.json";
        private const string HISTORY_DELIMITER = "yy-MM-dd";

        private readonly MetaClient metaClient;

        public HeroService(MetaClient metaClient)
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
            foreach (var player in match.players)
            {
                await UpdateSummary(match, player, binder, log);
                await UpdateDetails(match, player, binder, log);
            }
        }

        private static async Task UpdateSummary(Match match, Player player, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(SUMMARY_PATH, player.hero_id));
            var summary = await ReadData<HeroSummary>(binder, attr);

            var victory = match.Victory(player);
            var date = DateTimeOffset.FromUnixTimeSeconds(match.start_time);
            var bounds = date.AddDays(-6);
            var mid = date.AddDays(-3);

            summary.Total.Picks++;
            summary.Total.Wins += victory ? 1 : 0;

            summary.Data.Add(new HeroSummaryVictory() { Timestamp = date, Victory = victory });
            summary.Data.RemoveAll(_ => _.Timestamp < bounds);

            var current = summary.Data.Where(_ => _.Timestamp > mid).ToList();
            var previous = summary.Data.Where(_ => _.Timestamp < mid).ToList();
            summary.Current.Picks = current.Count();
            summary.Current.Wins = current.Count(_ => _.Victory == true);
            summary.Previous.Picks = previous.Count;
            summary.Previous.Wins = previous.Count(_ => _.Victory == true);

            await WriteData(binder, attr, summary);
        }

        private static async Task UpdateDetails(Match match, Player player, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(DETAILS_PATH, player.hero_id));
            var details = await ReadData<HeroCombo>(binder, attr);

            var victory = match.Victory(player);

            if (player.ability_upgrades != null)
            {
                var abilities = player.ability_upgrades.Select(_ => _.ability).Distinct().ToList();
                foreach (var id in abilities)
                {
                    var item = details.Data.Find(_ => _.Id == id);
                    if (item == null)
                    {
                        item = new HeroComboStat() { Id = id };
                        details.Data.Add(item);
                    }

                    item.Update(victory);
                }
            }

            await WriteData(binder, attr, details);
        }

        public async Task<List<HeroSummaryDelta>> GetSummary(IBinder binder, ILogger log)
        {
            var collection = new List<HeroSummaryDelta>();

            var heroes = this.metaClient.GetHeroes();
            foreach (var hero in heroes)
            {
                var attr = new BlobAttribute(string.Format(SUMMARY_PATH, hero.Id));
                var summary = await ReadData<HeroSummaryDelta>(binder, attr);
                collection.Add(summary);
            }

            return collection;
        }

        public async Task<HeroDetail> GetDetails(int id, IBinder binder, ILogger log)
        {
            var skills = metaClient.GetSkills();
            var heroes = metaClient.GetHeroes();
            var hero = heroes.Find(_ => _.Id == id);
            if(hero == null)
                throw new ArgumentOutOfRangeException(nameof(id));

            var summary = await ReadData<HeroSummary>(binder, new BlobAttribute(string.Format(SUMMARY_PATH, id)));
            var combos = await ReadData<HeroCombo>(binder, new BlobAttribute(string.Format(DETAILS_PATH, id)));
            var factory = new HeroAttributeFactory(heroes, hero);

            var details = new HeroDetail();
            details.Id = hero.Id;
            details.Name = hero.Name;
            details.Image = hero.ImageBanner;
            details.Attributes = factory.GetAttributes();
            details.Picks = summary.Total.Picks;
            details.Wins = summary.Total.Wins;
            details.History = summary.Data
                .GroupBy(_ => _.Timestamp.ToString(HISTORY_DELIMITER))
                .Select(_ => new HeroDetailHistory() 
                { 
                    Day = _.Key, 
                    Picks = _.Count(), 
                    Wins = _.Count(_ => _.Victory) 
                })
                .ToList();

            details.Talents = combos.Data
                .Join(hero.Talents, _ => _.Id, _ => _.Id, (lhs, rhs) => new HeroDetailCombo()
                {
                    Id = rhs.Id,
                    Name = rhs.Key,
                    Picks = lhs.Picks,
                    Wins = lhs.Wins,
                })
                .ToList();

            details.Abilities = combos.Data
                .Join(hero.Abilities, _ => _.Id, _ => _.Id, (lhs, rhs) => new HeroDetailCombo()
                {
                    Id = rhs.Id,
                    Name = rhs.Name,
                    Image  = rhs.Image,
                    Picks = lhs.Picks,
                    Wins = lhs.Wins,
                })
                .ToList();

            details.Combos = combos.Data
                .Join(skills, _ => _.Id, _ => _.Id, (lhs, rhs) => new HeroDetailCombo()
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