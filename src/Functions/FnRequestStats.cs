
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnRequestStats
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("RequestStats")]
        public static async Task<IActionResult> Run(
            // Request - Anonymous
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            // Stats
            [Blob("hgv-stats")]CloudBlobContainer statsContainer,
            // Logger
            TraceWriter log
        )
        {
            //log.Info($"Fn-RequestStats(): handing request at {DateTime.UtcNow}");

            string key = req.Query["key"];
            string type = req.Query["type"];

            var blob = statsContainer.GetBlockBlobReference($"{type}/{key}.json");
            var result = await blob.ExistsAsync();
            if (!result)
            {
                return new NotFoundResult();
            }
            else
            {
                var json = await blob.DownloadTextAsync();
                return new OkObjectResult(json);
            }
        }
    }
}
