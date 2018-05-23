
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
    public class AuthData
    {
        public string Persona { get; set; }
        public long SteamId { get; set; }
        public long DotaId { get; set; }
    }

    public static class FnPlayerSummary
    {
        [FunctionName("PlayerSummary")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter log)
        {
            var identity = req.Query["identity"].ToString();
            if(string.IsNullOrWhiteSpace(identity))
                return new BadRequestObjectResult("Please pass an [identity] as the 62 bit steam id of the player on the query string");

            var steamId = long.Parse(identity);

            var client = new HGV.Daedalus.DotaApiClient("BD0FBFBE762E542E3090A90D3C6D8E56");
            var player = await client.GetPlayerSummaries(steamId);

            var data = new AuthData();
            data.SteamId = steamId;
            data.DotaId = long.Parse(identity.Substring(3)) - 61197960265728;
            data.Persona = player.personaname;

            return new OkObjectResult(data);
        }
    }
}
