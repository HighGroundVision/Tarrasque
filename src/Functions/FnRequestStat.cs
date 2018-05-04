
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using HGV.Tarrasque.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HGV.Tarrasque.Functions
{
    public static class FnRequestStat
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("RequestStats")]
        public static async Task<IActionResult> Run(
            // Request
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            // Stats totals
            [Blob("hgv-stats/totals.json", System.IO.FileAccess.ReadWrite)] CloudBlockBlob totalsBlob,
            // Abilities table 
            [Table("HGVStatsAbilities")]CloudTable statsTables,
            // Logger
            TraceWriter log
        )
        {
            string key = req.Query["key"];

            // QueryString Gruad
            if (string.IsNullOrWhiteSpace(key))
                return new BadRequestObjectResult("Key is required");

            // Get Totals
            var jsonTotals = await totalsBlob.DownloadTextAsync();
            var totals = JsonConvert.DeserializeObject<Totals>(jsonTotals);
            var totalMatches = totals.Modes[(int)GameMode.ability_draft];

            // Get Range
            var operation = TableOperation.Retrieve<AbilityStats>("Range", key);
            var result = await statsTables.ExecuteAsync(operation);
            var range = result.Result as AbilityStats;

            // Get Melee
            operation = TableOperation.Retrieve<AbilityStats>("Melee", key);
            result = await statsTables.ExecuteAsync(operation);
            var melee = result.Result as AbilityStats;

            // Convert to Stats Summaries
            var root = new StatsDetail();
            root.Abilities = key.Split('-').Select(_ => int.Parse(_)).ToList();
            root.Melee = ConvertStatToSummary(melee, totalMatches);
            root.Range = ConvertStatToSummary(range, totalMatches);

            // Return reponse
            return new OkObjectResult(root);
        }

        private static AbilitySummary ConvertStatToSummary(AbilityStats table, int totalMatches)
        {
            if (table == null) return null;

            return new AbilitySummary
            {
                Wins = table.Wins,
                Picks = table.Picks,
                Kills = table.Kills,
                Deaths = table.Deaths,
                Assists = table.Assists,
                Damage = table.Damage,
                Destruction = table.Destruction,
                Gold = table.Gold,
                PickRate = table.Picks / (float)totalMatches,
                WinRate = table.Wins / (float)table.Picks,
                Total = totalMatches
            };
        }
    }
}
