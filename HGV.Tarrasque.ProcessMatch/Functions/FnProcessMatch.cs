using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.ProcessMatch.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Functions
{
    public class FnProcessMatch
    {
        private readonly IProcessMatchService _matchService;

        public FnProcessMatch(IProcessMatchService matchService)
        {
            _matchService = matchService;
        }

        [FunctionName("FnProcessMatch")]
        public async Task Process(
            [QueueTrigger("hgv-ad-matches")]Match match,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnProcessMatch", log))
            {
                await _matchService.ProcessMatch(match, binder, log);
            }
        }
    }
}