using HGV.Basilius;
using HGV.Tarrasque.API.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Functions
{
    public class FnDailyCounts
    {
        [FunctionName("FnDailyModes")]
        public async Task DailyModes(
            [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,
            [Blob("hgv-modes/{datetime:yyyy-MM-dd-HH}.json")] TextWriter writer,
            [DurableClient] IDurableEntityClient client,
            ILogger log
        )
        {
            log.LogInformation("FnDailyModes");

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

            foreach (var item in collection.Entities)
            {
                await client.SignalEntityAsync<IModeEntity>(item.EntityId, proxy => proxy.Delete());
            }

            var json = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(json);
        }

        [FunctionName("FnDailyRegions")]
        public async Task DailyRegions(
            [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,
            [Blob("hgv-regions/{datetime:yyyy-MM-dd-HH}.json")] TextWriter writer,
            [DurableClient] IDurableEntityClient client,
           ILogger log
        )
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

            foreach (var item in collection.Entities)
            {
                await client.SignalEntityAsync<IRegionEntity>(item.EntityId, proxy => proxy.Delete());
            }

            var json = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(json);
        }

        [FunctionName("FnDailyHeroes")]
        public async Task DailyHeroes(
           [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,
           [Blob("hgv-heroes/{datetime:yyyy-MM-dd-HH}.json")] TextWriter writer,
           [DurableClient] IDurableEntityClient client,
           ILogger log
        )
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

            foreach (var item in collection.Entities)
            {
                await client.SignalEntityAsync<IHeroEntity>(item.EntityId, proxy => proxy.Delete());
            }

            var json = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(json);

        }
    }
}
