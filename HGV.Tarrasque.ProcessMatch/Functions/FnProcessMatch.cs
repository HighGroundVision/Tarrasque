using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.ProcessMatch.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
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
            [QueueTrigger("hgv-ad-matches")]Match item,
            IBinder binder,
            ILogger log)
        {
            try
            {
                await _service.ProcessMatch(item, binder, log);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}