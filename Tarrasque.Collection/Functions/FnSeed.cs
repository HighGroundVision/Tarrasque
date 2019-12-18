using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using HGV.Tarrasque.Collection.Services;

namespace HGV.Tarrasque.Collection.Functions
{
    public class FnSeed
    {
        private readonly ISeedService _service;

        public FnSeed(ISeedService service)
        {
            _service = service;
        }

        [FunctionName("FnSeed")]
        public async Task<IActionResult> SeedCheckPoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "seed")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            ILogger log)
        {
            try
            {
                await _service.Seed(writer);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                return new StatusCodeResult(500);
            }
        }
    }
}
