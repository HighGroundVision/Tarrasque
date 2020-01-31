using HGV.Basilius;
using HGV.Tarrasque.API.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                    _.State
                }).ToList();

            var modes = MetaClient.Instance.Value.GetModes();
            var data = modes.Join(states, _ => _.Key, _ => _.Id, (lhs, rhs) => new
            {
                Id = lhs.Key,
                Name = lhs.Value,
                Total = (int)rhs.State["total"],
            })
            .OrderByDescending(_ => _.Total)
            .ToList();

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
                    _.State
                }).ToList();

            var regions = MetaClient.Instance.Value.GetRegions();
            var data = regions.Join(states, _ => _.Key, _ => _.Id, (lhs, rhs) => new
            {
                Id = lhs.Key,
                Name = lhs.Value,
                Total = (int)rhs.State["total"],
            })
            .GroupBy(_ => _.Name)
            .Select(_ => new
            {
                Name = _.Key,
                Total = _.Sum(x => x.Total),
            })
            .OrderByDescending(_ => _.Total)
            .ToList();

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
                    _.State 
                }).ToList();

            var heroes = MetaClient.Instance.Value.GetADHeroes();
            var data = heroes.Join(states, _ => _.Id, _ => _.Id, (lhs, rhs) => new
            {
                Id = lhs.Id,
                Name = lhs.Name,
                Wins = (int)rhs.State["wins"],
                Losses = (int)rhs.State["losses"],
                Total = (int)rhs.State["total"],
            })
            .OrderByDescending(_ => _.Total)
            .ToList();

            return new OkObjectResult(data);
        }

        [FunctionName("FnGetHeropPairsCounts")]
        public async Task<IActionResult> GetHeropPairsCounts(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hero/{key}")] HttpRequest req,
          [DurableClient] IDurableEntityClient client,
          string key,
          ILogger log)
        {

            var entityId = new EntityId(nameof(HeroPairEntity), key);
            var state = await client.ReadEntityStateAsync<HeroPairEntity>(entityId);
            if (state.EntityExists == false)
                return new NotFoundResult();
            
            return new OkObjectResult(state.EntityState.Collection);
        }

        [FunctionName("FnGetAccountCounts")]
        public async Task<IActionResult> GetAccountCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts/{key}")] HttpRequest req,
           [Blob("hgv-ad-players/{key}.json")]TextReader reader,
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

