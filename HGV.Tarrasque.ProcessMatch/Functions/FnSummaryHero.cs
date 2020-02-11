using HGV.Basilius;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessMatch.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch
{
    public class FnSummaryHero
    {
        public FnSummaryHero()
        {
        }

        [FunctionName("FnSummaryHero")]
        public async Task Process(
            [QueueTrigger("hgv-summary-heroes")]string timestamp,
            [DurableClient]IDurableClient client,
            IBinder binder,
            ILogger log)
        {
            var heroes = MetaClient.Instance.Value.GetADHeroes()
                .Select(_ => new { _.Id, _.Name })
                .OrderBy(_ => _.Id)
                .ToList();
            
            foreach (var hero in heroes)
            {
                var id = new EntityId(nameof(HeroEntity), $"{hero.Id}");
                var state = await client.ReadEntityStateAsync<HeroEntity>(id);
                await client.SignalEntityAsync<IHeroEntity>(id, _ => _.Reset());

                var model = new HeroModel()
                {
                    HeroId = hero.Id,
                    HeroName = hero.Name,
                    Timestamp = timestamp,
                    Total = state.EntityState?.Total ?? 0,
                    Wins = state.EntityState?.Wins ?? 0
                };

                var path = $"hgv-heroes/{timestamp}/{model.HeroId:000}.json";
                var attr = new BlobAttribute(path);
                var writer = await binder.BindAsync<TextWriter>(attr);
                var json = JsonConvert.SerializeObject(model);
                await writer.WriteAsync(json);
            }
        }

    }
}