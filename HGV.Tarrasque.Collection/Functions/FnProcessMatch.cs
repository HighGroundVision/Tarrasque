using HGV.Tarrasque.Collection.Models;
using HGV.Tarrasque.Collection.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Collection.Functions
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
            [QueueTrigger("hgv-ad-matches")]MatchReference item,
            [Blob("hgv-matches/{Match}.json")]TextWriter writerMatch,         
            [Blob("hgv-regions/{Region}.json")]TextReader readerRegion,
            [Blob("hgv-regions/{Region}.json")]TextWriter writerRegion,
            [Blob("hgv-heroes/{Date}/{Region}/heroes.json")]TextReader readHeroes,
            [Blob("hgv-heroes/{Date}/{Region}/heroes.json")]TextWriter writerHeroes,
            [Blob("hgv-abilities/{Date}/{Region}/abilities.json")]TextReader readAbilities,
            [Blob("hgv-abilities/{Date}/{Region}/abilities.json")]TextWriter writerAbilties,
            [Queue("hgv-accounts")]IAsyncCollector<AccountReference> queueAccounts,
            ILogger log)
        {
            var match = await _service.FetchMatch(item.Match);

            await _service.StoreMatch(match, writerMatch);
            await _service.UpdateRegion(match, readerRegion, writerRegion);
            await _service.UpdateHeroes(match, readHeroes, writerHeroes);
            await _service.UpdateAbilities(match, readAbilities, writerAbilties);
            await _service.QueueAccounts(match, queueAccounts);
        }
    }
}

