using HGV.Daedalus;
using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Common.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Services
{
    public interface IPlayerService
    {
        Task RegisterPlayer(long accountId, IBinder binder, ILogger log);
        Task<List<PlayerModel>> GetGlobalLeaderboard(CloudTable table, ILogger log);
        Task<List<PlayerModel>> GetRegionalLeaderboard(int region, CloudTable table, ILogger log);
    }

    public class PlayerService : IPlayerService
    {
        private readonly IDotaApiClient dotaApiClient;

        public PlayerService(IDotaApiClient dotaApiClient)
        {
            this.dotaApiClient = dotaApiClient;
        }

        public async Task RegisterPlayer(long accountId, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute($"hgv-players/{accountId}.json");
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);
            var exists = await blob.ExistsAsync();
            if (exists == true)
                return;

            var model = new PlayerModel() { AccountId = accountId };
            var json = JsonConvert.SerializeObject(model);
            await blob.UploadTextAsync(json);
        }

        public async Task<List<PlayerModel>> GetGlobalLeaderboard(CloudTable table, ILogger log)
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

            return models;
        }

        public async Task<List<PlayerModel>> GetRegionalLeaderboard(int region, CloudTable table, ILogger log)
        {
            var query = new TableQuery<PlayerEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, $"{region}"),
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

            return models;
        }
    }
}
