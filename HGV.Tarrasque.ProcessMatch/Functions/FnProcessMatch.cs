using HGV.Tarrasque.ProcessMatch.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Functions
{
    public class FnProcessMatch
    {
        private readonly IProcessMatchService _service;

        public FnProcessMatch(IProcessMatchService service)
        {
            _service = service;
        }

        [FunctionName("FnProcessMatch")]
        public async Task Process(
            [QueueTrigger("hgv-ad-analysis")]MatchReference item,
            [Blob("hgv-matches/{Match}.json")]TextReader readerMatch,
            [Queue("hgv-regions")]IAsyncCollector<RegionReference> queueRegions,
            [Queue("hgv-heroes")]IAsyncCollector<HeroReference> queueHeroes,
            [Queue("hgv-hero-abilities")]IAsyncCollector<HeroAbilityReference> queueHeroAbilities,
            [Queue("hgv-abilities")]IAsyncCollector<AbilityReference> queueAbilities,
            [Queue("hgv-accounts")]IAsyncCollector<AccountReference> queueAccounts,
            ILogger log)
        {
            var match = await _service.ReadMatch(readerMatch);
            await _service.QueueRegions(match, queueRegions);
            await _service.QueueHeroes(match, queueHeroes);
            await _service.QueueHeroAbilities(match, queueHeroAbilities);
            await _service.QueueAbilities(match, queueAbilities);
            await _service.QueueAccounts(match, queueAccounts);
        }
    }
}

