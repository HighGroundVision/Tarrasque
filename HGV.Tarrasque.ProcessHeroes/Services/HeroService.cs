using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.ProcessHeroes.Models;
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
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessHeroes.Services
{
    public interface IHeroService
    {
        Task Process(Match match, IBinder binder, ILogger log);
        Task<List<DTO.HeroSummary>> GetSummary(IBinder binder, ILogger log);
        Task<DTO.HeroDetail> GetDetails(int id, IBinder binder, ILogger log);
        Task UpdateSummary(IBinder binder, ILogger log);
    }

    public class HeroService : IHeroService
    {
        private const string SUMMARY_PATH = "hgv-heroes/summary.json";
        private const string HISTORY_PATH = "hgv-heroes/{0}/history.json";
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
            if (reader == null)
                throw new NullReferenceException(nameof(reader));

            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(input))
                throw new NullReferenceException(nameof(reader));

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
            foreach (var player in match.players)
            {
                await UpdateHistory(match, player, binder, log);
                await UpdateDetails(match, player, binder, log);
            }
        }

        private async Task UpdateHistory(Match match, Player player, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(HISTORY_PATH, player.hero_id));
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            Action<HeroHistory> updateFn = (history) =>
            {
                var victory = match.Victory(player);
                var date = DateTimeOffset.FromUnixTimeSeconds(match.start_time);
                var bounds = date.AddDays(-6);
                var mid = date.AddDays(-3);

                history.Total.Picks++;
                history.Total.Wins += victory ? 1 : 0;

                history.Data.Add(new HeroHistoryVictory() { Timestamp = date, Victory = victory });
                history.Data.RemoveAll(_ => _.Timestamp < bounds);

                var current = history.Data.Where(_ => _.Timestamp > mid).ToList();
                var previous = history.Data.Where(_ => _.Timestamp < mid).ToList();
                history.Current.Picks = current.Count();
                history.Current.Wins = current.Count(_ => _.Victory == true);
                history.Previous.Picks = previous.Count;
                history.Previous.Wins = previous.Count(_ => _.Victory == true);
            };
            await ProcessBlob(blob, updateFn, log);
        }

        private async Task UpdateDetails(Match match, Player player, IBinder binder, ILogger log)
        {
            if (player.ability_upgrades == null)
                return;

            var attr = new BlobAttribute(string.Format(DETAILS_PATH, player.hero_id));
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            Action<HeroCombo> updateFn = (details) =>
            {
                var victory = match.Victory(player);
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
            };
            await ProcessBlob(blob, updateFn, log);
        }

        public async Task UpdateSummary(IBinder binder, ILogger log)
        {
            var blob = await binder.BindAsync<CloudBlockBlob>(new BlobAttribute(SUMMARY_PATH));

            var data = new Dictionary<int, HeroHistoryData>();
            var heroes = this.metaClient.GetADHeroes();
            foreach (var hero in heroes)
            {
                try
                {
                    var attr = new BlobAttribute(string.Format(HISTORY_PATH, hero.Id));
                    var history = await ReadData<HeroHistory>(binder, attr);
                    data.Add(hero.Id, history);
                }
                catch (Exception)
                {
                }
            }

            Action<HeroSummary> updateFn = (summary) => summary.Data = data;
            await ProcessBlob(blob, updateFn, log);
        }

        public async Task<List<DTO.HeroSummary>> GetSummary(IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(SUMMARY_PATH));
            var summary = await ReadData<HeroSummary>(binder, attr);

            var collection = new List<DTO.HeroSummary>();
            var heroes = this.metaClient.GetHeroes();
            foreach (var hero in heroes)
            {
                if (summary.Data.ContainsKey(hero.Id) == false)
                    continue;

                var data = summary.Data[hero.Id];

                var item = new DTO.HeroSummary() 
                {
                    Id = hero.Id,
                    Name = hero.Name,
                    Image = hero.ImageIcon,
                    Total = new DTO.HeroSummaryHistory()
                    {
                        Picks = data.Total.Picks,
                        Wins = data.Total.Wins,
                    },
                    Current = new DTO.HeroSummaryHistory()
                    {
                        Picks = data.Current.Picks,
                        Wins = data.Current.Wins,
                    },
                    Previous = new DTO.HeroSummaryHistory()
                    {
                        Picks = data.Current.Picks,
                        Wins = data.Current.Wins,
                    },
                };
                collection.Add(item);
            }

            return collection;
        }

        public async Task<DTO.HeroDetail> GetDetails(int id, IBinder binder, ILogger log)
        {
            var skills = metaClient.GetSkills();
            var heroes = metaClient.GetHeroes();
            var hero = heroes.Find(_ => _.Id == id);
            if(hero == null)
                throw new ArgumentOutOfRangeException(nameof(id));

            var summary = await ReadData<HeroHistory>(binder, new BlobAttribute(string.Format(HISTORY_PATH, id)));
            var combos = await ReadData<HeroCombo>(binder, new BlobAttribute(string.Format(DETAILS_PATH, id)));
            var factory = new DTO.HeroAttributeFactory(heroes, hero);

            var details = new DTO.HeroDetail();
            details.Id = hero.Id;
            details.Name = hero.Name;
            details.Image = hero.ImageBanner;
            details.Attributes = factory.GetAttributes();
            details.Picks = summary.Total.Picks;
            details.Wins = summary.Total.Wins;
            details.History = summary.Data
                .GroupBy(_ => _.Timestamp.ToString(HISTORY_DELIMITER))
                .Select(_ => new DTO.HeroDetailHistory() 
                { 
                    Day = _.Key, 
                    Picks = _.Count(), 
                    Wins = _.Count(_ => _.Victory) 
                })
                .ToList();

            details.Talents = combos.Data
                .Join(hero.Talents, _ => _.Id, _ => _.Id, (lhs, rhs) => new DTO.HeroDetailCombo()
                {
                    Id = rhs.Id,
                    Name = rhs.Key,
                    Picks = lhs.Picks,
                    Wins = lhs.Wins,
                })
                .ToList();

            details.Abilities = combos.Data
                .Join(hero.Abilities, _ => _.Id, _ => _.Id, (lhs, rhs) => new DTO.HeroDetailCombo()
                {
                    Id = rhs.Id,
                    Name = rhs.Name,
                    Image  = rhs.Image,
                    Picks = lhs.Picks,
                    Wins = lhs.Wins,
                })
                .ToList();

            details.Combos = combos.Data
                .Join(skills, _ => _.Id, _ => _.Id, (lhs, rhs) => new DTO.HeroDetailCombo()
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