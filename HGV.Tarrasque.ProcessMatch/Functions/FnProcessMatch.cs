using HGV.Tarrasque.ProcessMatch.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using HGV.Daedalus.GetMatchDetails;
using Newtonsoft.Json;

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
            //[Blob("hgv-regions/{Region}.json")]TextReader readerRegion,
            //[Blob("hgv-regions/{Region}.json")]TextWriter writerRegion,
            //[Blob("hgv-heroes/{Region}/{Date}/heroes.json")]TextReader readHeroes,
            //[Blob("hgv-heroes/{Region}/{Date}/heroes.json")]TextWriter writerHeroes,
            //[Blob("hgv-abilities/{Region}/{Date}/abilities.json")]TextReader readAbilities,
            //[Blob("hgv-abilities/{Region}/{Date}/abilities.json")]TextWriter writerAbilties,
            [Queue("hgv-regions")]IAsyncCollector<RegionReference> queueRegions,
            [Queue("hgv-heroes")]IAsyncCollector<HeroReference> queueHeroes,
            [Queue("hgv-abilities")]IAsyncCollector<AbilityReference> queueAbilities,
            [Queue("hgv-accounts")]IAsyncCollector<AccountReference> queueAccounts,
            ILogger log)
        {
            var match = await _service.ReadMatch(readerMatch);

            // TODO: Convert to Queues
            // await _service.UpdateRegion(match, readerRegion, writerRegion);
            // await _service.UpdateHeroes(match, readHeroes, writerHeroes);
            // await _service.UpdateAbilities(match, readAbilities, writerAbilties);

            await _service.QueueRegions(match, queueRegions);
            await _service.QueueHeroes(match, queueHeroes);
            await _service.QueueAbilities(match, queueAbilities);

            await _service.QueueAccounts(match, queueAccounts);
        }
    }
}

