using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Algorithms;
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
    public interface IPlayerService
    {
        Task Process(Match item, IBinder binder, ILogger log);
        Task UpdateLeaderboards(IBinder binder, ILogger log);
        Task<List<DTO.Summary>> GetSummaries(int id, IBinder binder, ILogger log);
        Task<DTO.Details> GetDetails(int id, IBinder binder, ILogger log);
        Task<DTO.Leaderboard> GetRegionalLeaderboard(int id, IBinder binder, ILogger log);
        Task<DTO.Leaderboard> GetGlobalLeaderboard(IBinder binder, ILogger log);
    }

    public class PlayerService : IPlayerService
    {
        private const string DETAILS_PATH = "hgv-heroes/{0}/details.json";
        private const string LEADERBOARD_PATH = "hgv-leaderboards/{0}.json";
        private const string PLAYER_ENTITY_TABLE = "HGVPlayers";
        private const ulong CATCH_ALL_ACCOUNT = 4294967295;

        private readonly MetaClient metaClient;

        public PlayerService(MetaClient metaClient)
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
            var calibrated = await UpdatePlayerSummary(match, binder, log);
            var players = match.players.Join(calibrated, _ => _.player_slot, _ => _, (lhs, rhs) => lhs).ToList();
            foreach (var player in players)
                await UpdateDetails(match, player, binder, log);
        }

        private async Task UpdateDetails(Match match, Player player, IBinder binder, ILogger log)
        {
            if (player.account_id == CATCH_ALL_ACCOUNT)
                return;

            var attr = new BlobAttribute(string.Format(DETAILS_PATH, player.account_id));
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            Action<PlayerDetail> updateFn = (model) =>
            {
                model.AccountId = player.account_id;
                model.Total++;

                var date = DateTimeOffset.FromUnixTimeSeconds(match.start_time);

                model.History.Add(new Models.History()
                {
                    MatchId = match.match_id,
                    Date = date,
                    Hero = player.hero_id,
                    Victory = match.Victory(player),
                    Abilities = player.GetAbilities().Select(_ => _.Id).ToList()
                });

                model.WinRate = (float)model.History.Count(_ => _.Victory) / (float)model.Total;

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
                        Abilities = p.GetAbilities().Select(_ => _.Id).ToList()
                    };

                    if (p.GetTeam() == player.GetTeam())
                        combatant.With.Add(history);
                    else
                        combatant.Against.Add(history);
                }
            };
            await ProcessBlob(blob, updateFn, log);
        }

        private async Task<List<int>> UpdatePlayerSummary(Match match, IBinder binder, ILogger log)
        {
            var calibrated = new List<int>();

            var table = await binder.BindAsync<CloudTable>(new TableAttribute(PLAYER_ENTITY_TABLE));

            var partitionKey = this.metaClient.GetRegionId(match.cluster).ToString();
            var filter = string.Empty;

            var players = match.players.Where(_ => _.account_id != CATCH_ALL_ACCOUNT).ToList();
            if (players.Count == 0)
                return calibrated;

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

                if(item.Data.Calibrated)
                    calibrated.Add(item.Player.player_slot);

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

            return calibrated;
        }

        public async Task UpdateLeaderboards(IBinder binder, ILogger log)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        /// <remarks>
        /// DELETE IN THE FUTURE
        /// </remarks>
        [Obsolete("Not a fan of this method but could be useful for debuging, so keeping it for now...", false)]
        public async Task<List<Summary>> GetSummaries(int id, IBinder binder, ILogger log)
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
                    Calibrated = _.Calibrated
                })
                .ToList();

            return collection;
        }

        public async Task<Details> GetDetails(int id, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(DETAILS_PATH, id));
            var data = await ReadData<PlayerDetail>(binder, attr);

            var details = new Details();
            return details;
        }

        public async Task<Leaderboard> GetRegionalLeaderboard(int id, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(LEADERBOARD_PATH, id));
            var data = await ReadData<LeaderboardDetails>(binder, attr);

            var leaderboard = new Leaderboard();
            return leaderboard;
        }

        public async Task<Leaderboard> GetGlobalLeaderboard(IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(LEADERBOARD_PATH, "global"));
            var data = await ReadData<LeaderboardDetails>(binder, attr);

            var leaderboard = new Leaderboard();
            return leaderboard;
        }
    }
}
