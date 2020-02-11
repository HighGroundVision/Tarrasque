using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.ProcessMatch.Entities;
using HGV.Tarrasque.ProcessMatch.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
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
            [QueueTrigger("hgv-ad-matches")]Match match,
            [DurableClient] IDurableEntityClient client,
            ILogger log)
        {
            await _service.ProcessMatch(match, client, log);
        }
    }
}