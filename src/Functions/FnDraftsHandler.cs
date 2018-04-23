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
        [FunctionName("DraftsHandler")]
        public static async Task Run(
            // Queue {Name}
            [QueueTrigger("hgv-drafts")]StatSnapshot item,
            // Stats for the Draft (Pre Seeded)
            [Blob("hgv-stats/drafts/{Key}.json")]CloudBlockBlob matchBlob,
            // Next for total matches
            [Blob("hgv-master/next.json", System.IO.FileAccess.Read)]string nextJson,
            // Logger
            TraceWriter log
        )
        {
            log.Info($"Fn-DraftsHandler({item.Key}): started at {DateTime.UtcNow}");

            // Blob Gruad - Make sure Blob has been seeded
            var result = await matchBlob.ExistsAsync();
            if (result == false)
            {
                log.Warning($"Fn-DraftsHandler({item.Key}): No draft found matching the key.");
                return;
            }

            // Get [Next] for total matches
            var next = JsonConvert.DeserializeObject<Next>(nextJson);
            var totalMatches = next.TotalMatches;
            // TODO: move out into another file

            // add the snapshot to the stats
            await ProcessSnapshot(log, matchBlob, item, totalMatches);
        }

        private static async Task ProcessSnapshot(TraceWriter log, CloudBlockBlob matchBlob, StatSnapshot item, float totalMatches)
        {
            string leaseId = String.Empty;
            try
            {
                leaseId = await matchBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(15));
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
            finally
            {
                if (string.IsNullOrEmpty(leaseId))
                {
                    await matchBlob.ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(leaseId));
                }
            }
        }
    }
}
