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
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Polly;
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
        Task<List<DTO.Summary>> GetSummaries(int id, IBinder binder, ILogger log);
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

        private async Task UpdateLeaderboard(IBinder binder, ILogger log, int regionId, List<PlayerEntity> collectionRegional)
        {
            var attr = new BlobAttribute(string.Format(LEADERBOARD_PATH, regionId));
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            Action<LeaderboardDetails> updateFn = (model) =>
            {
                model = new LeaderboardDetails()
                {
                    Region = regionId,
                    List = collectionRegional.Select(_ => new LeaderboardEntity()
                    {
                        AccountId = uint.Parse(_.RowKey),
                        Ranking = _.Ranking,
                        Total = _.Total,
                        Wins = _.Wins,
                        WinRate = _.WinRate,
                    }).ToList()
                };
            };
            await ProcessBlob(blob, updateFn, log);
        }

        private async Task<List<Profile>> GetProfiles(params ulong[] identities)
        {
            try
            {
                return await dotaClient.GetPlayersSummary(identities.ToList());
            }
            catch (Exception)
            {
                return new List<Daedalus.GetPlayerSummaries.Player>();
            }
        }

        private async Task<List<Friend>> GetFriends(int id)
        {
            try
            {
                var steamId = (ulong)id + 76561197960265728L;
                var friends = await dotaClient.GetFriendsList(steamId);
                return friends;
            }
            catch (Exception)
            {
                return new List<Friend>();
            }
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
                // Unknown Region
                if (region.Key == 0) 
                    continue;

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
                        .Concat(segment.Results)
                        .OrderByDescending(_ => _.Ranking)
                        .Take(100)
                        .ToList();

                    collectionGlobal = collectionGlobal
                        .Concat(segment.Results)
                        .OrderByDescending(_ => _.Ranking)
                        .Take(100)
                        .ToList();
                }
                while (token != null);

                await UpdateLeaderboard(binder, log, region.Key, collectionRegional);
            }

            await UpdateLeaderboard(binder, log, GLOBAL_LEADERBOARD_REGION, collectionGlobal);
        }

        public async Task<List<Summary>> GetSummaries(int id, IBinder binder, ILogger log)
        {
            try
            {
                var table = await binder.BindAsync<CloudTable>(new TableAttribute(PLAYER_ENTITY_TABLE));
                var filter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, $"{id}");
                var result = await table.ExecuteQuerySegmentedAsync(new TableQuery<PlayerEntity>().Where(filter), null);
                var collection = result
                    .Select(_ => new Summary()
                    {
                        RegionId = int.Parse(_.PartitionKey),
                        AccountId = long.Parse(_.RowKey),
                        Total = _.Total,
                        Wins = _.Wins,
                        WinRate = _.WinRate,
                        Ranking = _.Ranking,
                        Calibrated = _.Calibrated
                    })
                    .ToList();

                return collection;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                return null;
            }
        }

        public async Task<Details> GetDetails(int id, IBinder binder, ILogger log)
        {
            try
            {
                var attr = new BlobAttribute(string.Format(DETAILS_PATH, id));
                var data = await ReadData<PlayerDetail>(binder, attr);

                var heroes = metaClient.GetHeroes();
                var skills = metaClient.GetSkills();

                // Get Player Summary
                var identities = data.Combatants.Select(_ => _.SteamId).Take(100).ToArray();
                var profiles = await GetProfiles(identities);

                // Get Friends List
                var friends = await GetFriends(id);

                var details = new Details();
                details.AccountId = id;

                var profile = await GetProfiles(details.SteamId);
                details.Persona = profile.Select(_ => _.personaname).FirstOrDefault();
                details.Avatar = profile.Select(_ => _.avatar).FirstOrDefault();

                details.History = data.History
                    .Join(heroes, _ => _.Hero, _ => _.Id, (lhs, rhs) => new DTO.History()
                    {
                        MatchId = lhs.MatchId,
                        Date = lhs.Date,
                        Victory = lhs.Victory,
                        Hero = new HeroSummary()
                        {
                            Id = rhs.Id,
                            Name = rhs.Name,
                            Image = rhs.ImageProfile
                        },
                        Abilities = lhs.Abilities.Join(skills, _ => _, _ => _.Id, (lhs, rhs) => new AbilitySummary
                        {
                            Id = rhs.Id,
                            Name = rhs.Name,
                            Image = rhs.Image,
                            IsUltimate = rhs.IsUltimate
                        }).OrderBy(_ => _.IsUltimate).ToList(),
                    }).ToList();

                details.Combatants = data.Combatants
                    .Join(profiles, _ => _.SteamId, _ => _.steamid, (player, profile) => new DTO.PlayerSummary()
                    {
                        AccountId = player.AccountId,
                        SteamId = player.SteamId,
                        Persona = profile.personaname,
                        Avatar = profile.avatar,
                        Friend = friends.Any(x => x.SteamId == player.SteamId),
                        With = player.With.Join(heroes, _ => _.Hero, _ => _.Id, (history, hero) => new DTO.History()
                        {
                            MatchId = history.MatchId,
                            Date = history.Date,
                            Victory = history.Victory,
                            Hero = new HeroSummary()
                            {
                                Id = hero.Id,
                                Name = hero.Name,
                                Image = hero.ImageProfile
                            },
                            Abilities = history.Abilities.Join(skills, _ => _, _ => _.Id, (id, ability) => new AbilitySummary()
                            {
                                Id = ability.Id,
                                Name = ability.Name,
                                Image = ability.Image,
                                IsUltimate = ability.IsUltimate
                            }).OrderBy(_ => _.IsUltimate).ToList(),
                        }).ToList(),
                        Against = player.Against.Join(heroes, _ => _.Hero, _ => _.Id, (history, hero) => new DTO.History()
                        {
                            MatchId = history.MatchId,
                            Date = history.Date,
                            Victory = history.Victory,
                            Hero = new HeroSummary()
                            {
                                Id = hero.Id,
                                Name = hero.Name,
                                Image = hero.ImageProfile
                            },
                            Abilities = history.Abilities.Join(skills, _ => _, _ => _.Id, (id, ability) => new AbilitySummary()
                            {
                                Id = ability.Id,
                                Name = ability.Name,
                                Image = ability.Image,
                                IsUltimate = ability.IsUltimate
                            }).OrderBy(_ => _.IsUltimate).ToList(),
                        }).ToList(),
                    })
                    .ToList();

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

            var identities = data.List.Select(_ => _.SteamId).ToArray();
            var profiles = await GetProfiles(identities);

            var collection = data.List.Join(profiles, _ => _.SteamId, _ => _.steamid, (data, profile) => new Leaderboard()
            {
                AccountId = data.AccountId,
                SteamId = data.SteamId,
                Persona = profile.personaname,
                Avatar = profile.avatar,
                Total = data.Total,
                Wins = data.Wins,
                WinRate = data.WinRate,
                Ranking = data.Ranking,
            }).ToList();

            return collection;
        }
    }
}
