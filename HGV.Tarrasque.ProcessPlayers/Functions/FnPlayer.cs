using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.ProcessPlayers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessPlayers
{
    public class FnPlayer
    {
        private readonly IPlayerService playerService;

        public FnPlayer(IPlayerService playerService)
        {
            this.playerService = playerService;
        }

        [FunctionName("FnPlayerProcess")]
        public async Task Run(
            [QueueTrigger("hgv-ad-players")]Match item,
            IBinder binder,
            ILogger log
        )
        {
            using (new Timer("FnPlayerProcess", log))
            {
                await this.playerService.Process(item, binder, log);
            }
        }

        [FunctionName("FnPlayerTimer")]
        public async Task PlayerTimer(
            [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,
            IBinder binder,
            ILogger log
        )
        {
            using (new Timer("FnPlayerTimer", log))
            {
                await this.playerService.UpdateLeaderboards(binder, log);
            }
        }

        [FunctionName("FnGlobalLeaderboard")]
        public async Task<IActionResult> GlobalLeaderboard(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leaderboard/global")] HttpRequest req,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnGlobalLeaderboard", log))
            {
                var leaderboard = await this.playerService.GetLeaderboard(0, binder, log);
                return new OkObjectResult(leaderboard);
            }
        }

        [FunctionName("FnRegionalLeaderboard")]
        public async Task<IActionResult> RegionalLeaderboard(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leaderboard/region/{id}")] HttpRequest req,
            int id,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnRegionalLeaderboard", log))
            {
                var leaderboard = await this.playerService.GetLeaderboard(id, binder, log);
                return new OkObjectResult(leaderboard);
            }
        }

        [FunctionName("FnPlayerSummary")]
        public async Task<IActionResult> PlayerSummary(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/summary/{id}")] HttpRequest req,
            int id,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnPlayerSummary", log))
            {
                var colleciton = await this.playerService.GetSummaries(id, binder, log);
                if (colleciton == null)
                    return new NotFoundResult();
                else
                    return new OkObjectResult(colleciton);
            }
        }

        [FunctionName("FnPlayerDetails")]
        public async Task<IActionResult> PlayerDetails(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/details/{id}")] HttpRequest req,
            int id,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnPlayerDetails", log))
            {
                var details = await this.playerService.GetDetails(id, binder, log);
                if (details == null)
                    return new NotFoundResult();
                else
                    return new OkObjectResult(details);
            }
        }
    }
}
