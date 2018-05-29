using HGV.Tarrasque.Models;
using HGV.Tarrasque.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnMatchHistory
    {
        [FunctionName("MatchHistory")]
        public async static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            [Blob("hgv-matches", FileAccess.Read)]CloudBlobContainer container, 
            TraceWriter log)
        {
            string accountQuery = req.Query["account"].ToString();
            if (string.IsNullOrWhiteSpace(accountQuery))
                return new BadRequestObjectResult("Please pass an [account] id on the query string");

            string modeQuery = req.Query["mode"].ToString();
            if (string.IsNullOrWhiteSpace(modeQuery))
                return new BadRequestObjectResult("Please pass a game [mode] on the query string");

            var accountId = long.Parse(accountQuery);
            var gameMode = int.Parse(modeQuery);

            var etag = new EntityTagHeaderValue($"\"{accountId}{DateTime.UtcNow.ToString("yyMMdd")}\"");
            if (ETagTest.Compare(req, etag))
                return new NotModifiedResult();

            var blob = container.GetBlockBlobReference($"{gameMode}/{accountId}.json");
            var result = await blob.ExistsAsync();
            if (result == false)
                return new NotFoundResult();

            var json = await blob.DownloadTextAsync();
            var matches = JsonConvert.DeserializeObject<List<RecentMatch>>(json);

            return new EtagOkObjectResult(matches) { ETag = etag };
        }
    }
}
