
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

namespace HGV.Tarrasque.Functions
{
    public static class FnMatchDetails
    {
        private static string APIKEY = "4932A809199A74AB6833EDFD9BADC176";

        [FunctionName("MatchDetails")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter log)
        {
            string matchQuery = req.Query["match"].ToString();
            if (string.IsNullOrWhiteSpace(matchQuery))
            {
                return new BadRequestObjectResult("Please pass a [match] id on the query string");
            }

            var matchId = long.Parse(matchQuery);

            var etag = new EntityTagHeaderValue($"\"{matchId}\"");
            if(ETagTest.Compare(req, etag))
                return new StatusCodeResult((int)System.Net.HttpStatusCode.NotModified);

            var client = new HGV.Daedalus.DotaApiClient(APIKEY);
            var match = await client.GetMatchDetails(matchId);

            return new EtagOkObjectResult(match) { ETag = etag };
        }
    }
}
