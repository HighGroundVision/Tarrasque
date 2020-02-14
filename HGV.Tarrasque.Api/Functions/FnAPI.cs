
using HGV.Tarrasque.Common.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HGV.Tarrasque.Api.Functions
{
    public class FnAPI
    {

        public FnAPI()
        {
        }

        [FunctionName("FnRegionCounts")]
        public IActionResult GetRegionCounts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "daily/region/{date}/{region}")] HttpRequest req,
            [Table("HGVRegions", "{date}", "{region}")]RegionEntity entity,
            ILogger log)
        {
            if (entity == null)
                return new NotFoundResult();

            var json = JsonConvert.SerializeObject(entity);
            return new OkObjectResult(json);
        }
    }
}
