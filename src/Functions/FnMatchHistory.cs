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

namespace HGV.Tarrasque.Functions
{
    public static class FnMatchHistory
    {
        [FunctionName("MatchHistory")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter log)
        {
            const int LIMIT = 10;

            string accountQuery = req.Query["account"].ToString();
            if (string.IsNullOrWhiteSpace(accountQuery))
                return new BadRequestObjectResult("Please pass an [account] id on the query string");

            var accountId = long.Parse(accountQuery);

            var client = new HGV.Daedalus.DotaApiClient("BD0FBFBE762E542E3090A90D3C6D8E56");
            var history = await client.GetMatchHistory(accountId);
            var limitedHistory = history.Take(LIMIT);

            var matches = new List<HGV.Daedalus.GetMatchHistory.Match>();
            foreach (var item in limitedHistory)
            {
                var match = await client.GetMatchDetails(item.match_id);
                if (match.game_mode == 18)
                {
                    matches.Add(item);
                }
            }

            return new OkObjectResult(matches);
        }
    }
}
