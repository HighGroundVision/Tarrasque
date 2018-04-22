using System;
using System.Linq;
using System.Threading.Tasks;
using HGV.Tarrasque.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace HGV.Tarrasque.Functions
{
    public class TestMessage
    {
        public string Day { get; set; }
    }

    public static class FnGatherADStatsAbilities
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("FnGatherADStatsAbilities")]
        public static async Task Run(
            // Queue with {day}
            [QueueTrigger("hgv-ad-stats-abilities")]String day,
            // AD Stats Tables
            [Table("HGVAdStatsAbilities")]CloudTable tableAbilities,
            // Blob with matches
            [Blob("hgv-matches/{queueTrigger}/18")]CloudBlobDirectory matchesDirectory,
            // Blob with stats (Needs to be seeded!)
            [Blob("hgv-stats/18/abilities")]CloudBlobDirectory statsDirectory,
            // Logger
            TraceWriter log
        )
        {
            log.Info($"FnGatherADStatsAbilities started at: {DateTime.Now}");

            var totalMatches = await CountMatches(matchesDirectory);

            var query = new TableQuery<AbilityCount>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, day)
            );

            TableContinuationToken continuationToken = null;
            do
            {
                var response = await tableAbilities.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (var item in response.Results)
                {
                    await ProcessItem(log, statsDirectory, totalMatches, item);
                }
            } while (continuationToken != null);
        }

        private static async Task ProcessItem(TraceWriter log, CloudBlobDirectory statsDirectory, int totalMatches, AbilityCount item)
        {
            var blob = statsDirectory.GetBlockBlobReference(item.RowKey);

            string leaseId = string.Empty;
            try
            {
                AbilityDraftStat stats;
                var exists = await blob.ExistsAsync();
                if (exists == true)
                {
                    leaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(15));
                    var jsonDonwload = await blob.DownloadTextAsync();
                    stats = JsonConvert.DeserializeObject<AbilityDraftStat>(jsonDonwload);
                }
                else
                {
                    stats = new AbilityDraftStat();
                    stats.Abilities.Add(item.AbilityId);
                }

                stats.Total += totalMatches;
                stats.Picks += item.Picks;
                stats.Wins += item.Wins;
                stats.Kills += item.Kills;
                stats.Deaths += item.Deaths;
                stats.Assist += item.Assist;
                stats.WinRate = (float)stats.Wins / (float)stats.Total;
                stats.PickRate = (float)stats.Picks / (float)stats.Total;

                var jsonUpload = JsonConvert.SerializeObject(stats);
                await blob.UploadTextAsync(jsonUpload);
            }
            catch (Exception ex)
            {
                log.Error("Error Processing stats", ex);
            }
            finally
            {
                if(string.IsNullOrWhiteSpace(leaseId))
                {
                    await blob.ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(leaseId));
                }
            }
        }

        private static async Task<int> CountMatches(CloudBlobDirectory directoryAll, int blockSize = 100)
        {
            var totalMatches = 0;

            BlobContinuationToken continuationToken = null;
            do
            {
                var response = await directoryAll.ListBlobsSegmentedAsync(true, BlobListingDetails.None, blockSize, continuationToken, null, null);
                continuationToken = response.ContinuationToken;
                totalMatches += response.Results.Count();
            } while (continuationToken != null);

            return totalMatches;
        }
    }
}
