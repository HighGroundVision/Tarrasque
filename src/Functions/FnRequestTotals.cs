
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HGV.Tarrasque.Functions
{
    public static class FnRequestTotals
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("RequestTotals")]
        public static async Task<IActionResult> Run(
            // Request
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            // Stats
            [Blob("hgv-stats/totals.json", System.IO.FileAccess.ReadWrite)]CloudBlockBlob totalsBlob,
            // Logger
            TraceWriter log
        )
        {
            var json = await totalsBlob.DownloadTextAsync();
            return new OkObjectResult(json);
        }
    }
}
