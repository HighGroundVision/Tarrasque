
using HGV.Basilius;
using HGV.Daedalus;
using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Common.Entities;
using HGV.Tarrasque.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Functions
{
    public class FnPlayerAPI
    {
        private readonly IDotaApiClient dotaApiClient;

        public FnPlayerAPI(IDotaApiClient dotaApiClient)
        {
            this.dotaApiClient = dotaApiClient;
        }

        [FunctionName("FnRegisterPlayer")]
        public async Task<IActionResult> RegisterPlayer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/register/{account}")] HttpRequest req,
            string account,
            [Blob("hgv-players/{account}.json")] TextWriter writer,
            ILogger log)
        {
            var accountId = long.Parse(account);
            var model = new PlayerModel() { AccountId = accountId };
            var output = JsonConvert.SerializeObject(model);
            await writer.WriteAsync(output);

            return new OkResult();
        }

        [FunctionName("FnLeaderboardGlobal")]
        public async Task<IActionResult> GetLeaderboardGlobal(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/leaderboard")] HttpRequest req,
            [Table("HGVPlayers")]CloudTable table,
            ILogger log)
        {
            var query = new TableQuery<PlayerEntity>().Where(
                TableQuery.GenerateFilterConditionForDouble("Ranking", QueryComparisons.GreaterThan, 1000.0)
            );
            var collection = new List<PlayerEntity>();
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync<PlayerEntity>(query, token);
                token = segment.ContinuationToken;
                collection = collection
                    .Concat(segment.Results)
                    .OrderByDescending(_ => _.Ranking)
                    .Take(100)
                    .ToList();
            }
            while (token != null);

            var list = collection.Select(_ => (ulong)_.SteamId).ToList();
            var profiles = await this.dotaApiClient.GetPlayersSummary(list);

            var models = collection
                .GroupJoin(profiles, _ => (ulong)_.SteamId, _ => _.steamid, (model, profile) => new { model, profile })
                .SelectMany(_ => _.profile.DefaultIfEmpty(), (x, profile) => new { Model = x.model, Profile = profile })
                .Select(_ => new PlayerModel()
                {
                    RegionId = int.Parse(_.Model.PartitionKey),
                    AccountId = _.Model.AccountId,
                    SteamId = _.Model.SteamId,
                    Total = _.Model.Total,
                    Ranking = _.Model.Ranking,
                    WinRate = _.Model.WinRate,
                    Wins = _.Model.Wins,
                    Losses = _.Model.Losses,
                    Persona = _.Profile?.personaname ?? string.Empty,
                })
                .ToList();

            return new OkObjectResult(models);
        }

        [FunctionName("FnLeaderboardByRegion")]
        public async Task<IActionResult> GetLeaderboardByRegion(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/leaderboard/{region}")] HttpRequest req,
            string region,
            [Table("HGVPlayers")]CloudTable table,
            ILogger log)
        {
            var query = new TableQuery<PlayerEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, region),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDouble("Ranking", QueryComparisons.GreaterThan, 1000.0)
                )
            );
  
            var collection = new List<PlayerEntity>();
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync<PlayerEntity>(query, token);
                token = segment.ContinuationToken;
                collection = collection
                    .Concat(segment.Results)
                    .OrderByDescending(_ => _.Ranking)
                    .Take(100)
                    .ToList();
            }
            while (token != null);

            var list = collection.Select(_ => (ulong)_.SteamId).ToList();
            var profiles = await this.dotaApiClient.GetPlayersSummary(list);

            var models = collection
                .GroupJoin(profiles, _ => (ulong)_.SteamId, _ => _.steamid, (model, profile) => new { model, profile })
                .SelectMany(_ => _.profile.DefaultIfEmpty(), (x, profile) => new { Model = x.model, Profile = profile })
                .Select(_ => new PlayerModel()
                {
                    RegionId = int.Parse(_.Model.PartitionKey),
                    AccountId = _.Model.AccountId,
                    SteamId = _.Model.SteamId,
                    Total = _.Model.Total,
                    Ranking = _.Model.Ranking,
                    WinRate = _.Model.WinRate,
                    Wins = _.Model.Wins,
                    Losses = _.Model.Losses,
                    Persona = _.Profile?.personaname ?? string.Empty,
                })
                .ToList();

            return new OkObjectResult(models);
        }

    }
}
