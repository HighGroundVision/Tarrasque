using System;
using System.Threading.Tasks;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.ProcessHeroes.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.ProcessHeroes
{
    public class FnHero
    {
        private readonly IHeroService heroService;

        public FnHero(IHeroService heroService)
        {
            this.heroService = heroService;
        }

        [FunctionName("FnHeroProcess")]
        public async Task HeroProcess(
            [QueueTrigger("hgv-ad-heroes")]Match item, 
            IBinder binder,
            ILogger log
        )
        {
            using (new Timer("FnHeroProcess", log))
            {
                await this.heroService.Process(item, binder, log);
            }
        }

        [FunctionName("FnHeroTimer")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,
            IBinder binder,
            ILogger log
        )
        {
            using (new Timer("FnHeroTimer", log))
            {
                await this.heroService.UpdateSummary(binder, log);
            }
        }

        [FunctionName("FnHeroSummary")]
        public async Task<IActionResult> HeroSummary(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "heroes")] HttpRequest req,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnHeroSummary", log))
            {
                var summary = await this.heroService.GetSummary(binder, log);
                return new OkObjectResult(summary);
            }
        }

        [FunctionName("FnHeroDetails")]
        public async Task<IActionResult> HeroDetails(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hero/{id}")] HttpRequest req,
            int id,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnHeroDetails", log))
            {
                var details = await this.heroService.GetDetails(id, binder, log);
                return new OkObjectResult(details);
            }
        }

        [FunctionName("FnDraftPool")]
        public IActionResult DraftPool(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "draft/pool")] HttpRequest req,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnDraftPool", log))
            {
                var pool = this.heroService.GetPool();
                return new OkObjectResult(pool);
            }
        }
    }
}
