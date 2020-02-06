using HGV.Basilius;
using HGV.Tarrasque.API.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Functions
{
    public class FnApi
    {

        [FunctionName("FnGetModesCounts")]
        public async Task<IActionResult> GetModesCounts(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "modes/{key}")] HttpRequest req,
           [Blob("hgv-modes/{key}.json")]TextReader reader,
           string key,
           ILogger log
        )
        {
            if (reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }
    }
}

