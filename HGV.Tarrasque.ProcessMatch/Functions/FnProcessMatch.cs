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
        private readonly IProcessPlayersService _playersService;

        public FnProcessMatch(IProcessMatchService matchService, IProcessPlayersService playersService)
        {
            _matchService = matchService;
            _playersService = playersService;
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
                // await _playersService.ProcessMatch(match, binder, log);
            }
        }
    }
}