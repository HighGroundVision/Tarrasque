using HGV.Tarrasque.StoreMatch.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.StoreMatch.Functions
{
    public class FnStoreMatch
    {
        private readonly IStoreMatchService _service;

        public FnStoreMatch(IStoreMatchService service)
        {
            _service = service;
        }

        [FunctionName("FnStoreMatch")]
        public async Task Process(
            [QueueTrigger("hgv-ad-matches")]MatchReference item,
            [Blob("hgv-matches/{Match}.json")]TextWriter writerMatch,
            [Queue("hgv-ad-analysis")]IAsyncCollector<MatchReference> queue,
            ILogger log)
        {
            var match = await _service.FetchMatch(item.Match);
            await _service.StoreMatch(match, writerMatch);
            await queue.AddAsync(item);
        }
    }
}

