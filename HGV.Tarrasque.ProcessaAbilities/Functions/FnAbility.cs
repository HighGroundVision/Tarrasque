using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.ProcessAbilities.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessAbilities
{
    public class FnAbility
    {
        private readonly IAbilityService abilityService;

        public FnAbility(IAbilityService abilityService)
        {
            this.abilityService = abilityService;
        }

        [FunctionName("FnAbilityProcess")]
        public async Task AbilityProcess(
            [QueueTrigger("hgv-ad-abilities")]Match item, 
            IBinder binder,
            ILogger log
        )
        {
            using (new Timer("FnAbilityProcess", log))
            {
                await this.abilityService.Process(item, binder, log);
            }
        }

        [FunctionName("FnAbilityTimer")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,
            IBinder binder,
            ILogger log
        )
        {
            using (new Timer("FnAbilityTimer", log))
            {
                await this.abilityService.UpdateSummary(binder, log);
            }
        }

        [FunctionName("FnAbilitySummary")]
        public async Task<IActionResult> AbilitySummary(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "abilities")] HttpRequest req,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnAbilitySummary", log))
            {
                var summary = await this.abilityService.GetSummary(binder, log);
                return new OkObjectResult(summary);
            }
        }

        [FunctionName("FnAbilityDetails")]
        public async Task<IActionResult> AbilityDetails(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ability/{id}")] HttpRequest req,
            int id,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnAbilityDetails", log))
            {
                var details = await this.abilityService.GetDetails(id, binder, log);
                return new OkObjectResult(details);
            }
        }
    }
}
