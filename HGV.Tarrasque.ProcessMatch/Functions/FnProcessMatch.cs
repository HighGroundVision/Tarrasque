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
            [QueueTrigger("hgv-ad-matches")]MatchReference item,
            IBinder binder,
            ILogger log)
        {
            var match = await _service.FetchMatch(item.MatchId);

            await _service.ProcessMatch(match, binder);
        }
    }
}

