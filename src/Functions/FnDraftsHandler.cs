using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnDraftsHandler
    {
        const int GAMEMODE_AD = 18;

        [FunctionName("DraftsHandler")]
        public static async Task Run(
            // Queue {Name}
            [QueueTrigger("hgv-drafts")]StatSnapshot item,
            // Stats for the Draft (Pre Seeded)
            [Blob("hgv-stats/drafts/{Key}.json")]CloudBlockBlob matchBlob,
            // stats totals
            [Blob("hgv-stats/totals.json", System.IO.FileAccess.ReadWrite)]TextReader totalsReader,
            // Logger
            TraceWriter log
        )
        {
            log.Info($"Fn-DraftsHandler({item.Key}): started at {DateTime.UtcNow}");

            // Blob Gruad
            var result = await matchBlob.ExistsAsync();
            if (result == false)
            {
                // Make sure Blob has been created
                await CreateIfNoneExists(log, item, matchBlob);
            }

            var serailizer = JsonSerializer.CreateDefault();
            var totals = (Totals)serailizer.Deserialize(totalsReader, typeof(Totals));
            var totalMatches = totals.Modes[GAMEMODE_AD];

            // add the snapshot to the stats
            await ProcessSnapshot(log, matchBlob, item, totalMatches);
        }

        private static async Task CreateIfNoneExists(TraceWriter log, StatSnapshot item, CloudBlockBlob matchBlob)
        {
            log.Warning($"Fn-DraftsHandler({item.Key}) No draft found; creating draft.");

            var json = JsonConvert.SerializeObject(new AbilitiesStats());
            await matchBlob.UploadTextAsync(json);
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
                stat.PickRate = stat.Picks /totalMatches;
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
                log.Error($"Fn-DraftsHandler({item.Key}): failed to update stats.", ex);
            }
        }
    }
}
