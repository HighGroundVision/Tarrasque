using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using HGV.Tarrasque.API.Entities;

namespace HGV.Tarrasque.API.Functions
{
    public class FnApi
    {
        [FunctionName("FnGetModeCounts")]
        public async Task<IActionResult> GetModeCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mode/{key}")] HttpRequest req,
           [DurableClient] IDurableEntityClient client,
           string key,
           ILogger log)
        {
            var entityId = new EntityId(nameof(MatchCounter), key);
            var state = await client.ReadEntityStateAsync<MatchCounter>(entityId);
            var data = new
            {
                Exists = state.EntityExists,
                Total = state.EntityExists ? state.EntityState.Value : 0,
            };

            return new OkObjectResult(data);
        }

        [FunctionName("FnGetRegionCounts")]
        public async Task<IActionResult> GetRegionCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "region/{key}")] HttpRequest req,
           [DurableClient] IDurableEntityClient client,
           string key,
           ILogger log)
        {
            var entityId = new EntityId(nameof(RegionsCounter), key);
            var state = await client.ReadEntityStateAsync<RegionsCounter>(entityId);
            var data = new
            {
                Exists = state.EntityExists,
                Total = state.EntityExists ? state.EntityState.Value : 0,
            };

            return new OkObjectResult(data);
        }

        [FunctionName("FnGetAccountCounts")]
        public async Task<IActionResult> GetAccountCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts/{key}")] HttpRequest req,
           [DurableClient] IDurableEntityClient client,
           string key,
           ILogger log)
        {
            var entityId = new EntityId(nameof(PlayersCounter), key);
            var state = await client.ReadEntityStateAsync<PlayersCounter>(entityId);
            var data = new
            {
                Exists = state.EntityExists,
                Wins = state.EntityExists ? state.EntityState.Wins : 0,
                Losses = state.EntityExists ? state.EntityState.Losses : 0,
                Total = state.EntityExists ? state.EntityState.Total : 0,
            };

            return new OkObjectResult(data);
        }
    }
}

