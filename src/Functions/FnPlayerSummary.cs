
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using HGV.Tarrasque.Models;
using HGV.Daedalus;

namespace HGV.Tarrasque.Functions
{
    public static class FnPlayerSummary
    {
        private static readonly DotaApiClient client = new DotaApiClient("4932A809199A74AB6833EDFD9BADC176");

        [FunctionName("PlayerSummary")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter log)
        {
            var identity = req.Query["identity"].ToString();
            if(string.IsNullOrWhiteSpace(identity))
                return new BadRequestObjectResult("Please pass an [identity] as the 62 bit steam id of the player on the query string");

            var steamId = long.Parse(identity);

            var player = await client.GetPlayerSummaries(steamId);

            var data = new ProfileData();
            data.steam_id = steamId;
            data.dota_id = long.Parse(identity.Substring(3)) - 61197960265728;
            data.persona = player.personaname;

            return new OkObjectResult(data);
        }
    }
}