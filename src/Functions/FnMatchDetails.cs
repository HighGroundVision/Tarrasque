
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using System.Linq;
using HGV.Tarrasque.Utilities;
using System.Net.Http;
using System;

namespace HGV.Tarrasque.Functions
{
    public static class FnMatchDetails
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("MatchDetails")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter log)
        {
            string matchQuery = req.Query["match"].ToString();
            if (string.IsNullOrWhiteSpace(matchQuery))
                return new BadRequestObjectResult("Please pass a [match] id on the query string");

            var matchId = long.Parse(matchQuery);
            var timestamp = DateTime.UtcNow.ToString("yyMMdd");

            var etag = new EntityTagHeaderValue($"\"{matchId}|{timestamp}\"");
            if (ETagTest.Compare(req, etag))
                return new NotModifiedResult();

            var json = await httpClient.GetStringAsync($"https://api.opendota.com/api/matches/{matchId}");

            return new EtagOkObjectResult(json) { ETag = etag };
        }
    }
}
