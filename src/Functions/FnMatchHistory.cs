using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Net.Http.Headers;
using HGV.Tarrasque.Utilities;

namespace HGV.Tarrasque.Functions
{
    public static class FnMatchHistory
    {
        [FunctionName("MatchHistory")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter log)
        {
            string accountQuery = req.Query["account"].ToString();
            if (string.IsNullOrWhiteSpace(accountQuery))
                return new BadRequestObjectResult("Please pass an [account] id on the query string");

            var accountId = long.Parse(accountQuery);

            var etag = new EntityTagHeaderValue($"\"{accountId}{DateTime.UtcNow.ToString("yyMMddHH")}\"");
            if (ETagTest.Compare(req, etag))
                return new StatusCodeResult((int)System.Net.HttpStatusCode.NotModified);

            var client = new HGV.Daedalus.DotaApiClient("BD0FBFBE762E542E3090A90D3C6D8E56");
            var history = await client.GetMatchHistory(accountId);
            var matches = new List<HGV.Daedalus.GetMatchHistory.Match>();
            foreach (var item in history)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                var match = await client.GetMatchDetails(item.match_id);
                if (match.game_mode == 18)
                    matches.Add(item);

                if (matches.Count >= 10)
                    break;
            }
            
            return new EtagOkObjectResult(matches) { ETag = etag };
        }
    }
}
