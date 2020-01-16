using Dawn;
using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Services
{
    public interface IProcessMatchService
    {
        Task ProcessMatch(MatchReference matchRef, IBinder binder);
    }

    public class ProcessMatchService : IProcessMatchService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public ProcessMatchService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = MetaClient.Instance.Value;
        }

        // Replace with durable functions
        // https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-dotnet-entities#accessing-entities-through-interfaces
        public async Task ProcessMatch(MatchReference matchRef, IBinder binder)
        {
            Guard.Argument(matchRef, nameof(matchRef)).NotNull().Member(_ => _.MatchId, _ => _.NotZero());
            Guard.Argument(binder, nameof(binder)).NotNull();

            var match = await FetchMatch(matchRef.MatchId);

            var tasks = new Task[]
            {
                ProcessRegion(match, binder),
                ProcessAccounts(match, binder),
                ProcessHeroes(match, binder),
                ProcessAbilities(match, binder),
            };

            Task.WaitAll(tasks);
        }

        #region Match

        private async Task<Match> FetchMatch(ulong matchId)
        {
            Guard.Argument(matchId, nameof(matchId)).Positive().NotZero();

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                });

            var match = await policy.ExecuteAsync<Match>(async () =>
            {
                var details = await this.apiClient.GetMatchDetails(matchId);
                return details;
            });

            return match;
        }

        #endregion

        #region Blobs

        private static async Task<T> ReadBlob<T>(IBinder binder, string path) where T : new()
        {
            var blob = await binder.BindAsync<CloudBlockBlob>(new BlobAttribute(path));
            var exists = await blob.ExistsAsync();
            if (exists == false)
                return new T();

            using (Stream stream = await blob.OpenReadAsync())
            using (StreamReader sr = new StreamReader(stream))
            using (JsonTextReader jtr = new JsonTextReader(sr))
            {
                JsonSerializer ser = new JsonSerializer();
                var data = ser.Deserialize<T>(jtr);
                if (data == null)
                    return new T();
                else
                    return data;
            }
        }

        private static async Task WriteBlob<T>(IBinder binder, T obj, string path) where T : class
        {
            var blob = await binder.BindAsync<CloudBlockBlob>(new BlobAttribute(path));

            using (Stream stream = await blob.OpenWriteAsync())
            using (StreamWriter sw = new StreamWriter(stream))
            using (JsonTextWriter jtw = new JsonTextWriter(sw))
            {
                JsonSerializer ser = new JsonSerializer();
                ser.Serialize(jtw, obj);
            }
        }

        #endregion

        #region Region

        private static async Task ProcessRegion(Match match, IBinder binder)
        {
            var policy = Policy
                       .Handle<Exception>()
                       .RetryAsync(5);

            await policy.ExecuteAsync(async () =>
            {
                var regionId = match.Region();
                var date = match.GetStart().ToString("yy-MM-dd");
                var path = $"hgv-regions/{date}/summary.json";

                var collection = await ReadBlob<List<RegionData>>(binder, path);

                var item = collection.Find(_ => _.Region == regionId);
                if (item == null)
                    collection.Add(new RegionData() { Region = regionId, Matches = 1 });
                else
                    item.Matches++;

                await WriteBlob(binder, collection, path);
            });
        }

        #endregion

        #region Players

        private const long CATCH_ALL_ACCOUNT = 4294967295;
        private async Task ProcessAccounts(Match match, IBinder binder)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(binder, nameof(binder)).NotNull();

            foreach (var player in match.players)
            {
                var accountId = player.account_id;
                if (accountId == CATCH_ALL_ACCOUNT)
                    continue;

                var policy = Policy
                       .Handle<Exception>()
                       .RetryAsync(5);

                await policy.ExecuteAsync(async () =>
                {
                    var path = $"hgv-accounts/{accountId}/data.json";
                    var data = await ReadBlob<AccountData>(binder, path);
                    UpdateAccountData(match, player, data);
                    await WriteBlob(binder, data, path);
                });
            }
        }

        private static void UpdateAccountData(Match match, Player player, AccountData data)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(7));
            var date = match.GetStart();

            var hero = player.GetHero();
            var skills = player.GetSkills();

            data.AccountId = player.account_id;
            data.SteamId = player.SteamId();
            data.Persona = player.persona;

            data.Timeline.Add(new MatchTimestamp()
            {
                Id = match.match_id,
                Date = date,
                Victory = match.Victory(player)
            });

            data.HeroTimestamps.Add(new HeroTimestamp()
            {
                HeroId = hero.Id,
                Name = hero.Name,
                Date = date,
                Victory = match.Victory(player)
            });

            foreach (var skill in skills)
            {
                data.AbilityTimestamps.Add(new AbilityTimestamp()
                {
                    AbilityId = skill.Id,
                    Name = skill.Name,
                    Date = date,
                    Victory = match.Victory(player)
                });
            }

            data.HeroSummarys = data.HeroTimestamps
                .Where(_ => _.Date > cutoff)
                .GroupBy(_ => _.HeroId)
                .Select(_ => new HeroSummary()
                {
                    HeroId = _.Key,
                    Total = _.Count(),
                    Wins = _.Count(_ => _.Victory),
                    Losses = _.Count(_ => !_.Victory),
                })
                .ToList();

            data.AbilitySummarys = data.AbilityTimestamps
                .Where(_ => _.Date > cutoff)
                .GroupBy(_ => _.AbilityId)
                .Select(_ => new AbilitySummary()
                {
                    AbilityId = _.Key,
                    Total = _.Count(),
                    Wins = _.Count(_ => _.Victory),
                    Losses = _.Count(_ => !_.Victory),
                })
                .ToList();
        }

        #endregion

        #region Heroes

        private async Task ProcessHeroes(Match match, IBinder binder)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(binder, nameof(binder)).NotNull();

            var date = match.GetStart().ToString("yy-MM-dd");
            var maxAssists = match.players.Max(_ => _.assists);
            var maxGold = match.players.Max(_ => _.gold);
            var maxKills = match.players.Max(_ => _.kills);
            var minDeaths = match.players.Min(_ => _.deaths);

            foreach (var player in match.players)
            {
                var heroId = player.hero_id;
                var path = $"hgv-heroes/{date}/{heroId}/data.json";

                var policy = Policy
                       .Handle<Exception>()
                       .RetryAsync(5);

                await policy.ExecuteAsync(async () =>
                {
                    var item = InitializeHeroData(match, player, maxAssists, maxGold, maxKills, minDeaths);
                    var data = await ReadBlob<HeroData>(binder, path);
                    UpdateHeroData(item, data);
                    await WriteBlob(binder, item, path);
                });
            }
        }

        private static void UpdateHeroData(HeroData item, HeroData data)
        {
            item.Total += data.Total;
            item.Wins += data.Wins;
            item.Losses += data.Losses;
            item.DraftOrder += data.DraftOrder;
            item.MaxAssists += data.MaxAssists;
            item.MaxKills += data.MaxKills;
            item.MinDeaths += data.MinDeaths;
        }

        private static HeroData InitializeHeroData(Match match, Player player, int maxAssists, int maxGold, int maxKills, int minDeaths)
        {
            var item = new HeroData();
            item.Total = 1;
            item.DraftOrder = player.DraftOrder();

            if (match.Victory(player))
                item.Wins++;
            else
                item.Losses++;

            if (player.assists == maxAssists)
                item.MaxAssists++;

            if (player.gold == maxGold)
                item.MaxGold++;

            if (player.kills == maxKills)
                item.MaxKills++;

            if (player.deaths == minDeaths)
                item.MinDeaths++;

            return item;
        }

  
        #endregion

        #region Abilities

        private async Task ProcessAbilities(Match match, IBinder binder)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(binder, nameof(binder)).NotNull();

            var date = match.GetStart().ToString("yy-MM-dd");
            var maxAssists = match.players.Max(_ => _.assists);
            var maxGold = match.players.Max(_ => _.gold);
            var maxKills = match.players.Max(_ => _.kills);
            var minDeaths = match.players.Min(_ => _.deaths);

            foreach (var player in match.players)
            {
                var skills = player.GetSkills();
                foreach (var skill in skills)
                {
                    var abilityId = skill.Id;
                    var path = $"hgv-abilities/{date}/{abilityId}/data.json";

                    var policy = Policy
                       .Handle<Exception>()
                       .RetryAsync(5);

                    await policy.ExecuteAsync(async () =>
                    {
                        var item = InitializeAbilityData(match, player, skill, maxAssists, maxGold, maxKills, minDeaths);
                        var data = await ReadBlob<AbilityData>(binder, path);
                        UpdateAbilityData(item, data);
                        await WriteBlob(binder, item, path);
                    });
                }
            }
        }

        private static void UpdateAbilityData(AbilityData item, AbilityData data)
        {
            item.Total += data.Total;
            item.Wins += data.Wins;
            item.Losses += data.Losses;
            item.DraftOrder += data.DraftOrder;
            item.MaxAssists += data.MaxAssists;
            item.MaxKills += data.MaxKills;
            item.MinDeaths += data.MinDeaths;
            item.HeroAbility += data.HeroAbility;
        }

        private static AbilityData InitializeAbilityData(Match match, Player player, Ability skill, int maxAssists, int maxGold, int maxKills, int minDeaths)
        {
            var item = new AbilityData();
            item.Total = 1;
            item.DraftOrder = player.DraftOrder();
            item.HeroAbility = skill.HeroId == player.hero_id ? 1 : 0;

            if (match.Victory(player))
                item.Wins++;
            else
                item.Losses++;

            if (player.assists == maxAssists)
                item.MaxAssists++;

            if (player.gold == maxGold)
                item.MaxGold++;

            if (player.kills == maxKills)
                item.MaxKills++;

            if (player.deaths == minDeaths)
                item.MinDeaths++;
            return item;
        }

        #endregion
    }
}
