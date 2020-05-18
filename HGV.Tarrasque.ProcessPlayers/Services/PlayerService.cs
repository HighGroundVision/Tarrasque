using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetFriendsList;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Algorithms;
using HGV.Tarrasque.Common.Exceptions;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.ProcessPlayers.DTO;
using HGV.Tarrasque.ProcessPlayers.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessPlayers.Services
{
    using Profile = Daedalus.GetPlayerSummaries.Player;

    public interface IPlayerService
    {
        Task Process(Match item, IBinder binder, ILogger log);
        Task UpdateLeaderboards(IBinder binder, ILogger log);
        Task<DTO.Details> GetDetails(int id, IBinder binder, ILogger log);
        Task<List<DTO.Leaderboard>> GetLeaderboard(int id, IBinder binder, ILogger log);
    }

    public class PlayerService : IPlayerService
    {
        private const string DETAILS_PATH = "hgv-players/{0}/details.json";
        private const string LEADERBOARD_PATH = "hgv-leaderboards/{0}.json";
        private const string PLAYER_ENTITY_TABLE = "HGVPlayers";
        private const ulong CATCH_ALL_ACCOUNT = 4294967295;
        public const int GLOBAL_LEADERBOARD_REGION = 0;
        private const int LEADERBOARD_RANK_CUTOFF = 1000;

        private readonly MetaClient metaClient;
        private readonly IDotaApiClient dotaClient;

        public PlayerService(MetaClient metaClient, IDotaApiClient dotaClient)
        {
            this.metaClient = metaClient;
            this.dotaClient = dotaClient;
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
        private async Task UpdatePlayerSummary(Match match, IBinder binder, ILogger log)
        {
            var table = await binder.BindAsync<CloudTable>(new TableAttribute(PLAYER_ENTITY_TABLE));

            var partitionKey = this.metaClient.GetRegionId(match.cluster).ToString();
            var filter = string.Empty;

            var players = match.players.Where(_ => _.account_id != CATCH_ALL_ACCOUNT).ToList();
            if (players.Count == 0)
                return;

            foreach (var player in players)
            {
                var f = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, $"{player.account_id}")
                );

                if (string.IsNullOrWhiteSpace(filter))
                    filter = f;
                else
                    filter = TableQuery.CombineFilters(filter, TableOperators.Or, f);
            }

            var query = new TableQuery<PlayerEntity>().Where(filter);
            var result = await table.ExecuteQuerySegmentedAsync(query, null);
            var collection = players
                .GroupJoin(result, _ => _.account_id, _ => uint.Parse(_.RowKey), (player, data) => new { player, data })
                .SelectMany(_ => _.data.DefaultIfEmpty(new PlayerEntity()
                {
                    PartitionKey = partitionKey,
                    RowKey = $"{_.player.account_id}",
                    Ranking = 1000,
                    Total = 0,
                    WinRate = 0,
                    Wins = 0,
                    Calibrated = false
                }), (lhs, rhs) => new {
                    Player = lhs.player,
                    Data = rhs,
                    Ranking = rhs.Ranking,
                })
                .ToList();

            var batch = new TableBatchOperation();
            foreach (var item in collection)
            {
                item.Data.Total++;
                item.Data.Calibrated = item.Data.Total > 10;

                var victory = match.Victory(item.Player);
                if (victory)
                    item.Data.Wins++;

                item.Data.WinRate = (double)item.Data.Wins / (double)item.Data.Total;

                var opponents = collection.Where(_ => _.Player.GetTeam() != item.Player.GetTeam());
                if (opponents.Count() > 0)
                {
                    var avgRanking = opponents.Average(_ => _.Ranking);
                    item.Data.Ranking = ELORankingSystem.Calucate(item.Ranking, avgRanking, victory);
                }

                batch.Add(TableOperation.InsertOrMerge(item.Data));
            }

            await table.ExecuteBatchAsync(batch);
        }
        private async Task UpdateDetails(Match match, IBinder binder, ILogger log)
        {
            foreach (var player in match.players)
            {
                if (player.account_id == CATCH_ALL_ACCOUNT)
                    continue;

                var attr = new BlobAttribute(string.Format(DETAILS_PATH, player.account_id));
                var blob = await binder.BindAsync<CloudBlockBlob>(attr);

                Action<PlayerDetail> updateFn = (model) =>
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(match.start_time);

                    model.History.Add(new Models.History()
                    {
                        MatchId = match.match_id,
                        Region = metaClient.GetRegionId(match.cluster),
                        Date = date,
                        Hero = player.hero_id,
                        Victory = match.Victory(player),
                        Abilities = player.GetAbilities(metaClient).Select(_ => _.Id).ToList()
                    });

                    foreach (var p in match.players)
                    {
                        if (p.account_id == CATCH_ALL_ACCOUNT)
                            continue;

                        if (p.account_id == player.account_id)
                            continue;

                        // If Exists
                        var combatant = model.Combatants.Find(_ => _.AccountId == p.account_id);
                        if (combatant == null)
                        {
                            combatant = new Models.PlayerSummary()
                            {
                                AccountId = p.account_id,
                            };
                            model.Combatants.Add(combatant);
                        }

                        var history = new Models.History()
                        {
                            MatchId = match.match_id,
                            Region = metaClient.GetRegionId(match.cluster),
                            Date = date,
                            Hero = p.hero_id,
                            Victory = match.Victory(p),
                            Abilities = p.GetAbilities(metaClient).Select(_ => _.Id).ToList()
                        };

                        if (p.GetTeam() == player.GetTeam())
                            combatant.With.Add(history);
                        else
                            combatant.Against.Add(history);
                    }
                };
                await ProcessBlob(blob, updateFn, log);
            }
        }
        private async Task UpdateLeaderboard(int regionId, List<PlayerEntity> players, IBinder binder, ILogger log)
        {
            var collection = players.Select(_ => new LeaderboardEntity()
            {
                RegionId = int.Parse(_.PartitionKey),
                AccountId = uint.Parse(_.RowKey),
                Ranking = _.Ranking,
                Total = _.Total,
                WinRate = _.WinRate,
            }).ToList();

            var identities = collection.Select(_ => _.SteamId).ToArray();
            var profiles = await GetProfiles(regionId, identities);

            if(profiles.Count() > 0)
            {
                collection = collection.Join(profiles, _ => _.SteamId, _ => _.steamid, (lhs, rhs) => new LeaderboardEntity()
                {
                    RegionId = lhs.RegionId,
                    AccountId = lhs.AccountId,
                    Ranking = lhs.Ranking,
                    Total = lhs.Total,
                    WinRate = lhs.WinRate,
                    Persona = rhs.personaname,
                    Avatar = rhs.avatarfull
                }).ToList();
            }
            
            var model = new LeaderboardDetails()
            {
                Region = regionId,
                List = collection
            };

            var attr = new BlobAttribute(string.Format(LEADERBOARD_PATH, regionId));
            var writer = await binder.BindAsync<TextWriter>(attr);
            var json = JsonConvert.SerializeObject(model);
            await writer.WriteAsync(json);
        }
        private async Task<List<Profile>> GetProfiles(int key, params ulong[] identities)
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
            var ttl = new AbsoluteTtl(DateTimeOffset.Now.Date.AddMinutes(5));
            var cachePolicy = Policy.CacheAsync(memoryCacheProvider, ttl, cxt => $"{cxt.OperationKey}:{key}");

            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                });

            var commonResilience = Policy.WrapAsync(cachePolicy, retryPolicy);

             var profiles = await Policy<List<Profile>>
               .Handle<Exception>()
               .FallbackAsync(new List<Profile>())
               .WrapAsync(commonResilience)
               .ExecuteAsync(() => dotaClient.GetPlayersSummary(identities.ToList()));

            return profiles;
        }
        private async Task<List<Friend>> GetFriends(ulong steamId)
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
            var ttl = new AbsoluteTtl(DateTimeOffset.Now.Date.AddMinutes(5));
            var cachePolicy = Policy.CacheAsync(memoryCacheProvider, ttl, cxt => $"{cxt.OperationKey}:{steamId}");

            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(3)
                });

            var commonResilience = Policy.WrapAsync(cachePolicy, retryPolicy);

            var friends = await Policy<List<Friend>>
              .Handle<Exception>()
              .FallbackAsync(new List<Friend>())
              .WrapAsync(commonResilience)
              .ExecuteAsync(() => dotaClient.GetFriendsList(steamId));

            return friends;
        }
        private async Task<List<Summary>> GetSummaries(int id, IBinder binder, ILogger log)
        {
            var collection = new List<Summary>();

            var regions = this.metaClient.GetRegionsMeta();
            foreach (var region in regions)
            {
                try
                {
                    var leaderboard = await ReadData<LeaderboardDetails>(binder, new BlobAttribute(string.Format(LEADERBOARD_PATH, region.id)));
                    var maxRanking = leaderboard.List.Max(_ => _.Ranking);
                    var maxMatches = leaderboard.List.Max(_ => _.Total);

                    var player = await binder.BindAsync<PlayerEntity>(new TableAttribute(PLAYER_ENTITY_TABLE, $"{region.id}", $"{id}"));
                    if (player == null)
                        continue;

                    var summary = new Summary()
                    {
                        RegionId = region.id,
                        RegionName = region.name,
                        RegionGroup = region.group,
                        Total = player.Total,
                        Wins = player.Wins,
                        WinRate = player.WinRate,
                        Ranking = player.Ranking,
                        Calibrated = player.Calibrated,
                        DeltaRaking = maxRanking - player.Ranking,
                        DeltaTotal = maxMatches - player.Total,
                    };
                    collection.Add(summary);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message);
                }
            }
           
            return collection.OrderByDescending(_ => _.Total).ToList();
        }


        public async Task Process(Match match, IBinder binder, ILogger log)
        {
            await UpdatePlayerSummary(match, binder, log);
            await UpdateDetails(match, binder, log);
        }
        public async Task UpdateLeaderboards(IBinder binder, ILogger log)
        {
            var table = await binder.BindAsync<CloudTable>(new TableAttribute(PLAYER_ENTITY_TABLE));

            var collectionGlobal = new List<PlayerEntity>();

            var regions = metaClient.GetRegions();
            foreach (var region in regions)
            {
                var filters = new List<string>()
                {
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, $"{region.Key}"),
                    TableQuery.GenerateFilterConditionForDouble("Ranking", QueryComparisons.GreaterThan, LEADERBOARD_RANK_CUTOFF),
                    TableQuery.GenerateFilterConditionForBool("Calibrated", QueryComparisons.Equal, true)
                };

                var filter = string.Empty;
                foreach (var f in filters)
                    filter = string.IsNullOrEmpty(filter) ? f : TableQuery.CombineFilters(filter, TableOperators.And, f);

                var query = new TableQuery<PlayerEntity>().Where(filter).Take(1000);

                var collectionRegional = new List<PlayerEntity>();
                TableContinuationToken token = null;
                do
                {
                    var segment = await table.ExecuteQuerySegmentedAsync(query, token);
                    token = segment.ContinuationToken;

                    collectionRegional = collectionRegional
                        .Union(segment.Results)
                        .OrderByDescending(_ => _.Ranking)
                        .Take(100)
                        .ToList();

                    collectionGlobal = collectionGlobal
                        .Union(segment.Results)
                        .OrderByDescending(_ => _.Ranking)
                        .Take(100)
                        .ToList();
                }
                while (token != null);

                await UpdateLeaderboard(region.Key, collectionRegional, binder, log);
            }

            var distinctCollectionGlobal = collectionGlobal
                .GroupBy(p => p.RowKey)
                .Select(g => g.OrderByDescending(_ => _.Ranking).First())
                .ToList();

            await UpdateLeaderboard(GLOBAL_LEADERBOARD_REGION, distinctCollectionGlobal, binder, log);
        } 
        public async Task<Details> GetDetails(int id, IBinder binder, ILogger log)
        {
            try
            {
                var attr = new BlobAttribute(string.Format(DETAILS_PATH, id));
                var data = await ReadData<PlayerDetail>(binder, attr);

                var heroes = metaClient.GetHeroes();
                var skills = metaClient.GetSkills();

                var details = new Details();
                details.AccountId = id;

                var combatants = data.Combatants
                    .OrderByDescending(_ => _.With.Count)
                    .ThenByDescending(_ => _.Against.Count)
                    .Take(99)
                    .ToList();

                var identities = combatants
                    .Select(_ => _.SteamId)
                    .Concat(new List<ulong>() { details.SteamId })
                    .ToArray();

                var profiles = await GetProfiles(id, identities);
                var self = profiles.Where(_ => _.steamid == details.SteamId);
                details.Persona = self.Select(_ => _.personaname).FirstOrDefault();
                details.Avatar = self.Select(_ => _.avatarfull).FirstOrDefault();

                var friends = await GetFriends(details.SteamId);

                details.Summaries = await GetSummaries(id, binder, log);

                var query = combatants
                    .Join(profiles, _ => _.SteamId, _ => _.steamid, (player, profile) => new
                    {
                        AccountId = player.AccountId,
                        SteamId = player.SteamId,
                        Persona = profile.personaname,
                        Avatar = profile.avatarfull,
                        Friend = friends.Any(x => x.SteamId == player.SteamId),
                        With = player.With,
                        Against = player.Against,
                    })
                    .ToList();

                details.Combatants = query
                    .Select(_ => new DTO.PlayerSummary()
                    {
                        AccountId = _.AccountId,
                        SteamId = _.SteamId,
                        Persona = _.Persona,
                        Avatar = _.Avatar,
                        Friend = _.Friend,
                        Total = _.With.Count + _.Against.Count,
                        With = _.With.Count,
                        VictoriesWith = _.With.Count(x=> x.Victory == true),
                        Against = _.Against.Count,
                        VictoriesAgainst = _.Against.Count(x => x.Victory == false)
                    })
                    .ToList();

                var heroSummaries = heroes
                    .Select(hero => new HeroSummary()
                    {
                        Id = hero.Id,
                        Name = hero.Name,
                        Image = hero.ImageIcon
                    })
                    .ToList();

   
                var with = query.SelectMany(_ => _.With.Select(x => new
                    {
                        AccountId = _.AccountId,
                        SteamId = _.SteamId,
                        Persona = _.Persona,
                        Avatar = _.Avatar,
                        Friend = _.Friend,
                        MatchId = x.MatchId,
                        Victory = x.Victory,
                        Hero = x.Hero,
                        Abilities = x.Abilities,
                    }))
                    .Join(heroes, _ => _.Hero, _ => _.Id, (lhs, rhs) => new PlayerHistory()
                    {
                        AccountId = lhs.AccountId,
                        SteamId = lhs.SteamId,
                        Persona = lhs.Persona,
                        Avatar = lhs.Avatar,
                        Friend = lhs.Friend,
                        MatchId = lhs.MatchId,
                        Victory = lhs.Victory,
                        Hero = new HeroSummary()
                        {
                            Id = rhs.Id,
                            Name = rhs.Name,
                            Image = rhs.ImageIcon,
                        },
                        Abilities = lhs.Abilities
                        .Join(skills, _ => _, _ => _.Id, (abilityId, ability) => new AbilitySummary
                        {
                            Id = ability.Id,
                            Name = ability.Name,
                            Image = ability.Image,
                            IsUltimate = ability.IsUltimate
                        })
                        .OrderBy(_ => _.IsUltimate)
                        .ToList()
                    })
                    .ToList();

                var against = query.SelectMany(_ => _.Against.Select(x => new
                    {
                        AccountId = _.AccountId,
                        SteamId = _.SteamId,
                        Persona = _.Persona,
                        Avatar = _.Avatar,
                        Friend = _.Friend,
                        MatchId = x.MatchId,
                        Victory = x.Victory,
                        Hero = x.Hero,
                        Abilities = x.Abilities,
                    }))
                    .Join(heroes, _ => _.Hero, _ => _.Id, (lhs, rhs) => new PlayerHistory()
                    {
                        AccountId = lhs.AccountId,
                        SteamId = lhs.SteamId,
                        Persona = lhs.Persona,
                        Avatar = lhs.Avatar,
                        Friend = lhs.Friend,
                        MatchId = lhs.MatchId,
                        Victory = lhs.Victory,
                        Hero = new HeroSummary()
                        {
                            Id = rhs.Id,
                            Name = rhs.Name,
                            Image = rhs.ImageIcon,
                        },
                        Abilities = lhs.Abilities
                        .Join(skills, _ => _, _ => _.Id, (abilityId, ability) => new AbilitySummary
                        {
                            Id = ability.Id,
                            Name = ability.Name,
                            Image = ability.Image,
                            IsUltimate = ability.IsUltimate
                        })
                        .OrderBy(_ => _.IsUltimate)
                        .ToList()
                    })
                    .ToList();

                foreach (var item in data.History)
                {
                    var history = new DTO.History();
                    history.MatchId = item.MatchId;
                    history.Date = item.Date;
                    history.Victory = item.Victory;

                    history.Hero = heroSummaries.Find(_ => _.Id == item.Hero);
                    history.Abilities = item.Abilities
                        .Join(skills, _ => _, _ => _.Id, (abilityId, ability) => new AbilitySummary
                        {
                            Id = ability.Id,
                            Name = ability.Name,
                            Image = ability.Image,
                            IsUltimate = ability.IsUltimate
                        })
                        .OrderBy(_ => _.IsUltimate)
                        .ToList();

                    history.With = with.Where(_ => _.MatchId == item.MatchId).ToList();
                    history.Against = against.Where(_ => _.MatchId == item.MatchId).ToList();

                    details.History.Add(history);
                }

                details.History = details.History.OrderByDescending(_ => _.Date).ToList();

                return details;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                return null;
            }
        }
        public async Task<List<Leaderboard>> GetLeaderboard(int id, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(LEADERBOARD_PATH, id));
            var data = await ReadData<LeaderboardDetails>(binder, attr);

            var regions = this.metaClient.GetRegionsMeta();

            var collection = data.List
                .Join(regions, _ => _.RegionId, _ => _.id, (lhs, rhs) => new Leaderboard()
                {
                    RegionGroup = rhs.group,
                    RegionName = rhs.name,
                    AccountId = lhs.AccountId,
                    SteamId = lhs.SteamId,
                    Persona = lhs.Persona,
                    Avatar = lhs.Avatar,
                    Total = lhs.Total,
                    WinRate = lhs.WinRate,
                    Ranking = lhs.Ranking,
                }).ToList();

            return collection;
        }
    }
}
