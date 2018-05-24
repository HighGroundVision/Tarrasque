
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnMatchDetails
    {
        [FunctionName("MatchDetails")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter log)
        {
            string matchQuery = req.Query["match"].ToString();
            if (string.IsNullOrWhiteSpace(matchQuery))
            {
                return new BadRequestObjectResult("Please pass a [match] id on the query string");
            }

            var matchId = long.Parse(matchQuery);

            var client = new HGV.Daedalus.DotaApiClient("BD0FBFBE762E542E3090A90D3C6D8E56");
            var match = await client.GetMatchDetails(matchId);

            return new OkObjectResult(match);
        }
    }
}
