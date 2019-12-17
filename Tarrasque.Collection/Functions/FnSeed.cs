using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HGV.Tarrasque.Collection.Functions
{
    public class FnSeed
    {
        private readonly IMyService _service;

        public FnSeed(IMyService service)
        {
            _service = service;
        }

        [FunctionName("FnSeed")]
        public async Task<IActionResult> SeedCheckPoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "seed")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            ILogger log)
        {
            await _service.Seed(writer);

            return new OkResult();
        }
    }
}
