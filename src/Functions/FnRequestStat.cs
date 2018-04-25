
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

namespace HGV.Tarrasque.Functions
{
    public static class FnRequestStat
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("RequestStats")]
        public static async Task<IActionResult> Run(
            // Request
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            // Abilities table 
            [Table("HGVStatsAbilities")]CloudTable statsTables,
            // Logger
            TraceWriter log
        )
        {
            string key = req.Query["key"];

            if (string.IsNullOrWhiteSpace(key))
                return new BadRequestObjectResult("Key is required");

            var operation = TableOperation.Retrieve<AbilityStats>("Range", key);
            var result = await statsTables.ExecuteAsync(operation);
            var range = result.Result as AbilityStats;

            operation = TableOperation.Retrieve<AbilityStats>("Melee", key);
            result = await statsTables.ExecuteAsync(operation);
            var melee = result.Result as AbilityStats;

            var root = new StatsDetail();
            root.Abilities = key.Split('-').Select(_ => int.Parse(_)).ToList();
            root.Melee = ConvertStatToSummary(melee);
            root.Range = ConvertStatToSummary(range);

            return new OkObjectResult(root);
        }

        private static AbilitySummary ConvertStatToSummary(AbilityStats table)
        {
            if (table == null)
            {
                return null;
            }
            else
            {
                var temp = new AbilitySummary();
                temp.Wins = table.Wins;
                temp.Picks = table.Picks;
                temp.Kills = table.Kills;
                temp.Deaths = table.Deaths;
                temp.Assists = table.Assists;
                temp.Damage = table.Damage;
                temp.Destruction = table.Destruction;
                temp.Gold = table.Gold;
                temp.PickVsTotal = table.PickVsTotal;
                temp.WinsVsTotal = table.WinsVsTotal;
                temp.WinsVsPicks = table.WinsVsPicks;
                return temp;
            }
        }
    }
}
