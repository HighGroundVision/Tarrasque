using HGV.Tarrasque.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnAbilityRanking
    {
        [FunctionName("AbilityRanking")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            [Blob("hgv-votes/abilities.json", FileAccess.Read)]CloudBlockBlob blob,
            TraceWriter log
        )
        {
            var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");
            var etag = new EntityTagHeaderValue($"\"{timestamp}\"");
            if (ETagTest.Compare(req, etag))
                return new NotModifiedResult();

            var josn = await blob.DownloadTextAsync();
            return new EtagOkObjectResult(josn) { ETag = etag };
        }
    }
}
