using HGV.Tarrasque.Collection.Models;
using HGV.Tarrasque.Collection.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

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
        public void Process(
            [QueueTrigger("hgv-ad-matches")]MatchReference item,
            [Blob("hgv-matches/{Match}.json", FileAccess.ReadWrite)]TextWriter writerMatch,
            [Blob("hgv-regions/{Date}/{Region}.json", FileAccess.Read)]TextReader readerRegion,
            [Blob("hgv-regions/{Date}/{Region}.json", FileAccess.Write)]TextWriter writerRegion,
            [Blob("hgv-heroes/{Date}/{Region}/heroes.json", FileAccess.Read)]TextReader readHeroes,
            [Blob("hgv-heroes/{Date}/{Region}/heroes.json", FileAccess.Write)]TextWriter writerHeroes,
            [Blob("hgv-abilities/{Date}/{Region}/abilities.json", FileAccess.Read)]TextReader readAbilities,
            [Blob("hgv-abilities/{Date}/{Region}/abilities.json", FileAccess.Write)]TextWriter writerAbilties,
            [Queue("hgv-accounts")]IAsyncCollector<AccountReference> queueAccounts,
            ILogger log)
        {
            _service.FetchMatch(item.Match);
            _service.StoreMatch(writerMatch);
            _service.UpdateRegion(readerRegion, writerRegion);
            _service.UpdateHeroes(readHeroes, writerHeroes);
            _service.UpdateAbilities(readAbilities, writerAbilties);
            _service.QueueAccounts(queueAccounts);
        }
    }
}

