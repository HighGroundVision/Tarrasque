using System;
using System.IO;
using System.Threading.Tasks;
using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace HGV.Tarrasque.Functions
{
    public static class FnAbilitiesHandler
    {
        const int GAMEMODE_AD = 18;

        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("AbilitiesHandler")]
        public static async Task Run(
            // Queue {Name}
            [QueueTrigger("hgv-abilities")]StatSnapshot item,
            // Stats for the Draft (Pre Seeded)
            [Blob("hgv-stats/abilities/{Key}.json")]CloudBlockBlob statsBlob,
            // stats totals
            [Blob("hgv-stats/totals.json", System.IO.FileAccess.ReadWrite)]CloudBlockBlob totalsBlob,
            // Logger
            TraceWriter log
        )
        {
            //log.Info($"Fn-AbilitiesHandler({item.Key}): started at {DateTime.UtcNow}");

            // Blob Gruad
            var result = await statsBlob.ExistsAsync();
            if (result == false)
            {
                // Make sure Blob has been created
                await CreateIfNoneExists(log, item, statsBlob);
            }

            var jsonTotals = await totalsBlob.DownloadTextAsync();
            var totals = JsonConvert.DeserializeObject<Totals>(jsonTotals);
            var totalMatches = totals.Modes[GAMEMODE_AD];

            // add the snapshot to the stats
            await ProcessSnapshot(log, statsBlob, item, totalMatches);
        }

        private static async Task CreateIfNoneExists(TraceWriter log, StatSnapshot item, CloudBlockBlob matchBlob)
        {
            try
            {
                log.Warning($"Fn-AbilitiesHandler({item.Key}) No ability found; creating ability.");

                var json = JsonConvert.SerializeObject(new AbilitiesStats());
                await matchBlob.UploadTextAsync(json);
            }
            catch (Exception ex)
            {
                log.Error($"Fn-AbilitiesHandler({item.Key}): failed to create stats.", ex);
            }
        }

        private static async Task ProcessSnapshot(TraceWriter log, CloudBlockBlob matchBlob, StatSnapshot item, float totalMatches)
        {
            try
            {
                var jsonDown = await matchBlob.DownloadTextAsync();
                var stat = JsonConvert.DeserializeObject<AbilitiesStats>(jsonDown);

                stat.Wins += item.Win ? 1 : 0;
                stat.WinRate = stat.Wins / totalMatches;
                stat.Picks++;
                stat.PickRate = stat.Picks / totalMatches;
                stat.Kills += item.Kills;
                stat.Deaths += item.Deaths;
                stat.Assists += item.Assists;
                stat.Destruction += item.Destruction;
                stat.Damage += item.Damage;
                stat.Gold += item.Gold;

                var jsonUp = JsonConvert.SerializeObject(stat);
                await matchBlob.UploadTextAsync(jsonUp);
            }
            catch (Exception ex)
            {
                log.Error($"Fn-AbilitiesHandler({item.Key}): failed to update stats.", ex);
            }
        }
    }
}
