using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.ProcessRegions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessRegions
{
    public class FnRegion
    {
        private readonly IRegionService regionService;

        public FnRegion(IRegionService regionService)
        {
            this.regionService = regionService;
        }

        [FunctionName("FnRegionProcess")]
        public async Task RegionProcess(
            [QueueTrigger("hgv-ad-regions")]Match item, 
            IBinder binder,
            ILogger log
        )
        {
            using (new Timer("FnRegionProcess", log))
            {
                await this.regionService.Process(item, binder, log);
            }
        }

        [FunctionName("FnRegionSummary")]
        public async Task<IActionResult> RegionSummary(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "regions")] HttpRequest req,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnRegionSummary", log))
            {
                var summary = await this.regionService.GetSummary(binder, log);
                return new OkObjectResult(summary);
            }
        }

        [FunctionName("FnRegionHistory")]
        public async Task<IActionResult> RegionHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "region/{region}/history")] HttpRequest req,
            int region,
            IBinder binder,
            ILogger log)
        {
            using (new Timer("FnRegionHistory", log))
            {
                var summary = await this.regionService.GetHistory(region, binder, log);
                return new OkObjectResult(summary);
            }
        }
    }
}
