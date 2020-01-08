using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.TriggerAggregates.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HGV.Tarrasque.TriggerAggregates
{
    public class FnTriggerAggregates
    {
        private readonly ITriggerAggregatesService _service;

        public FnTriggerAggregates(ITriggerAggregatesService service)
        {
            _service = service;
        }

        [FunctionName("FnTriggerAggregates")]
        public async Task Trigger(
            [TimerTrigger("0 0 1 * * *")]TimerInfo timer, // at 1:00 AM every day
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
