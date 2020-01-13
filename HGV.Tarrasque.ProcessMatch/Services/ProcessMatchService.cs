using Dawn;
using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
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

        public async Task ProcessMatch(MatchReference matchRef, IBinder binder)
        {
            Guard.Argument(matchRef, nameof(matchRef)).NotNull().Member(_ => _.MatchId, _ => _.NotZero());
            Guard.Argument(binder, nameof(binder)).NotNull();

            var match = await FetchMatch(matchRef.MatchId);

            await ProcessRegion(match, binder);
            await ProcessAccounts(match, binder);
            await ProcessHeroes(match, binder);
            await ProcessAbilities(match, binder);
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

        #region Region

        private static async Task ProcessRegion(Match match, IBinder binder)
        {
            var regionId = match.Region();
            var date = match.GetStart().ToString("yy-MM-dd");

            var collection = await ReadRegionSummary(binder, regionId, date);
            UpdateRegionSummary(regionId, collection);
            await WriteRegionSummary(binder, date, collection);
        }

        private static void UpdateRegionSummary(int regionId, List<RegionData> collection)
        {
            var item = collection.Find(_ => _.Region == regionId);
            if (item == null)
                collection.Add(new RegionData() { Region = regionId, Matches = 1 });
            else
                item.Matches++;
        }

        private static async Task<List<RegionData>> ReadRegionSummary(IBinder binder, int regionId, string date)
        {
            var reader = await binder.BindAsync<TextReader>(new BlobAttribute($"hgv-regions/{date}/summary.json"));
            if (reader == null)
                return new List<RegionData>();

            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(input))
                return new List<RegionData>();

            return JsonConvert.DeserializeObject<List<RegionData>>(input);
        }

        private static async Task WriteRegionSummary(IBinder binder, string date, List<RegionData> collection)
        {
            var output = JsonConvert.SerializeObject(collection);

            var writer = await binder.BindAsync<TextWriter>(new BlobAttribute($"hgv-regions/{date}/summary.json"));
            await writer.WriteAsync(output);
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
                if (player.account_id == CATCH_ALL_ACCOUNT)
                    continue;

                var data = await GetAccountData(binder, player);
                await UpdateProfileData(player, data);
                UpdateAccountData(match, player, data);
                await WriteAccountData(binder, player, data);
            }
        }

        private async Task UpdateProfileData(Player player, AccountData data)
        {
            var profile = await GetProfile(player.SteamId());
            data.AccountId = player.account_id;
            data.SteamId = profile.steamid;
            data.Persona = profile.personaname;
            data.Avatar = profile.avatar;
        }

        private async Task<AccountData> GetAccountData(IBinder binder, Player player)
        {
            var reader = await binder.BindAsync<TextReader>(new BlobAttribute($"hgv-accounts/{player.account_id}/data.json"));
            if (reader == null)
                return new AccountData();

            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(input))
                return new AccountData();

            return JsonConvert.DeserializeObject<AccountData>(input);
        }

        private async Task<HGV.Daedalus.GetPlayerSummaries.Player> GetProfile(ulong steamId)
        {
            Guard.Argument(steamId, nameof(steamId)).Positive().NotZero();

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                });

            var player = await policy.ExecuteAsync<HGV.Daedalus.GetPlayerSummaries.Player>(async () =>
            {
                return await this.apiClient.GetPlayerSummary(steamId);
            });

            return player;
        }

        private static void UpdateAccountData(Match match, Player player, AccountData data)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(7));
            var date = match.GetStart();

            var hero = player.GetHero();
            var skills = player.GetSkills();

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

        private static async Task WriteAccountData(IBinder binder, Player player, AccountData data)
        {
            var output = JsonConvert.SerializeObject(data);

            var writer = await binder.BindAsync<TextWriter>(new BlobAttribute($"hgv-accounts/{player.account_id}/data.json"));
            await writer.WriteAsync(output);
        }

        #endregion

        #region Heroes

        private async Task ProcessHeroes(Match match, IBinder binder)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(binder, nameof(binder)).NotNull();

            var date = match.GetStart().ToString("yy-MM-dd");

            foreach (var player in match.players)
            {
                var heroId = player.hero_id;
                var item = InitializeHeroData(match, player);
                var data = await GetExistingHeroData(binder, date, heroId);
                item += data;
                await WriteHeroData(binder, date, heroId, item);
            }
        }
      
        private static HeroData InitializeHeroData(Match match, Player player)
        {
            var maxAssists = match.players.Max(_ => _.assists);
            var maxGold = match.players.Max(_ => _.gold);
            var maxKills = match.players.Max(_ => _.kills);
            var minDeaths = match.players.Min(_ => _.deaths);

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

        private static async Task<HeroData> GetExistingHeroData(IBinder binder, string date, int heroId)
        {
            var policy = Policy
             .Handle<Exception>()
             .WaitAndRetryAsync(3, n => TimeSpan.FromSeconds(1));

            var reader = await binder.BindAsync<TextReader>(new BlobAttribute($"hgv-heroes/{date}/{heroId}/data.json"));
            if (reader == null)
                return new HeroData();

            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(input))
                return new HeroData();

            return JsonConvert.DeserializeObject<HeroData>(input);
        }

        private static async Task WriteHeroData(IBinder binder, string date, int heroId, HeroData item)
        {
            var output = JsonConvert.SerializeObject(item);

            var writer = await binder.BindAsync<TextWriter>(new BlobAttribute($"hgv-heroes/{date}/{heroId}/data.json"));
            await writer.WriteAsync(output);
        }

        #endregion

        #region Abilities

        private async Task ProcessAbilities(Match match, IBinder binder)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(binder, nameof(binder)).NotNull();

            var date = match.GetStart().ToString("yy-MM-dd");

            foreach (var player in match.players)
            {
                var skills = player.GetSkills();
                foreach (var skill in skills)
                {
                    var abilityId = skill.Id;
                    var item = InitializeAbilityData(match, player, skill);
                    var data = await GetExistingAbilityData(binder, date, abilityId);
                    item += data;
                    await WriteAbilityData(binder, date, abilityId, item);
                }
            }
        }

        private static AbilityData InitializeAbilityData(Match match, Player player, Ability skill)
        {
            var maxAssists = match.players.Max(_ => _.assists);
            var maxGold = match.players.Max(_ => _.gold);
            var maxKills = match.players.Max(_ => _.kills);
            var minDeaths = match.players.Min(_ => _.deaths);

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

        private static async Task<AbilityData> GetExistingAbilityData(IBinder binder, string date, int abilityId)
        {
            var reader = await binder.BindAsync<TextReader>(new BlobAttribute($"hgv-abilities/{date}/{abilityId}/data.json"));
            if (reader == null)
                return new AbilityData();

            var input = await reader.ReadToEndAsync();
            if (input == null)
                return new AbilityData();

            return JsonConvert.DeserializeObject<AbilityData>(input);
        }

        private static async Task WriteAbilityData(IBinder binder, string date, int abilityId, AbilityData item)
        {
            var output = JsonConvert.SerializeObject(item);

            var writer = await binder.BindAsync<TextWriter>(new BlobAttribute($"hgv-abilities/{date}/{abilityId}/data.json"));
            await writer.WriteAsync(output);
        }

        #endregion
    }
}
