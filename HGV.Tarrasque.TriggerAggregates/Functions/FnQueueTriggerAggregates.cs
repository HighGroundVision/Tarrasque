using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.TriggerAggregates.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HGV.Tarrasque.TriggerAggregates
{
    public class FnQueueTriggerAggregates
    {
        private readonly ITriggerAggregatesService _service;

        public FnQueueTriggerAggregates(ITriggerAggregatesService service)
        {
            _service = service;
        }

        [FunctionName("FnQueueTriggerAggregates")]
        public async Task Trigger(
            [QueueTrigger("hgv-aggregates-trigger")]string msg,
            [Queue("hgv-aggregates-heroes")]IAsyncCollector<HeroAggregateReference> queueHeroes,
            [Queue("hgv-aggregates-hero-abilities")]IAsyncCollector<HeroAggregateReference> queueHeroAbilties,
            [Queue("hgv-aggregates-abilities")]IAsyncCollector<AbilityAggregateReference> queueAbilties,
            ILogger log
        )
        {
            await _service.QueueHeroes(queueHeroes);
            await _service.QueueHeroAbilities(queueHeroAbilties);
            await _service.QueueAbilities(queueAbilties);
        }
    }
}
