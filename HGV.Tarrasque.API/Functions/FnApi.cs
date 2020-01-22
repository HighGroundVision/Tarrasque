using System;
using System.Linq;
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
using HGV.Basilius;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace HGV.Tarrasque.API.Functions
{
    public class FnApi
    {

        [FunctionName("FnGetModesCounts")]
        public async Task<IActionResult> GetModesCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "modes")] HttpRequest req,
           [DurableClient] IDurableEntityClient client,
           ILogger log)
        {
            var query = new EntityQuery()
            {
                EntityName = nameof(ModeEntity),
                FetchState = true,
                PageSize = 50
            };
            var collection = await client.ListEntitiesAsync(query, CancellationToken.None);
            var states = collection.Entities
                .Select(_ => new {
                    Id = int.Parse(_.EntityId.EntityKey),
                    State = _.State
                }).ToList();

            var modes = MetaClient.Instance.Value.GetModes();
            var data = modes.Join(states, _ => _.Key, _ => _.Id, (lhs, rhs) => new
            {
                Id = lhs.Key,
                Name = lhs.Value,
                Total = (int)rhs.State["value"],
            }).ToList();

            return new OkObjectResult(data);
        }

        [FunctionName("FnGetRegionsCounts")]
        public async Task<IActionResult> GetRegionsCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "regions")] HttpRequest req,
           [DurableClient] IDurableEntityClient client,
           ILogger log)
        {
            var query = new EntityQuery()
            {
                EntityName = nameof(RegionEntity),
                FetchState = true,
                PageSize = 50
            };
            var collection = await client.ListEntitiesAsync(query, CancellationToken.None);
            var states = collection.Entities
                .Select(_ => new {
                    Id = int.Parse(_.EntityId.EntityKey),
                    State = _.State
                }).ToList();

            var regions = MetaClient.Instance.Value.GetRegions();
            var data = regions.Join(states, _ => _.Key, _ => _.Id, (lhs, rhs) => new
            {
                Id = lhs.Key,
                Name = lhs.Value,
                Total = (int)rhs.State["value"],
            }).ToList();

            return new OkObjectResult(data);
        }

        [FunctionName("FnGetHeroesCounts")]
        public async Task<IActionResult> GetHeroesCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "heroes")] HttpRequest req,
           [DurableClient] IDurableEntityClient client,
           ILogger log)
        {
            var query = new EntityQuery()
            {
                EntityName = nameof(HeroEntity),
                FetchState = true,
                PageSize = 150
            };
            var collection = await client.ListEntitiesAsync(query, CancellationToken.None);
            var states = collection.Entities
                .Select(_ => new { 
                    Id = int.Parse(_.EntityId.EntityKey), 
                    State = _.State 
                }).ToList();

            var heroes = MetaClient.Instance.Value.GetADHeroes();
            var data = heroes.Join(states, _ => _.Id, _ => _.Id, (lhs, rhs) => new
            {
                Id = lhs.Id,
                Name = lhs.Name,
                Wins = (int)rhs.State["wins"],
                Losses = (int)rhs.State["losses"],
            }).ToList();

            return new OkObjectResult(data);
        }

        [FunctionName("FnGetAccountCounts")]
        public async Task<IActionResult> GetAccountCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts/{key}")] HttpRequest req,
           [Blob("hgv-players/{key}.json")]TextReader reader,
           string key,
           ILogger log)
        {
            if(reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }
    }
}

