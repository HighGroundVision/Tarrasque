using System;
using System.Linq;
using System.Threading.Tasks;
using HGV.Tarrasque.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace HGV.Tarrasque.Functions
{
    public static class FnGatherADStatsAbilities
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("FnGatherADStatsAbilities")]
        public static async Task Run(
            // Queue with {day}
            [QueueTrigger("hgv-ad-stats-abilities")]string day,
            // AD Stats Tables
            [Table("hgv-ad-stats-abilities")]CloudTable tableAbilities,
            // Blob with matches
            [Blob("hgv-matches/{queueTrigger}/18")]CloudBlobDirectory matchesDirectory,
            // Blob with stats
            [Blob("hgv-stats/18/abilities/")]CloudBlobDirectory statsDirectory,
            // Logger
            TraceWriter log
        )
        {
            log.Info($"FnGatherADStatsAbilities started at: {DateTime.Now}");

            var totalMatches = await CountMatches(matchesDirectory);

            var query = new TableQuery<AbilityADStat>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, day));

            TableContinuationToken continuationToken = null;
            do
            {
                var response = await tableAbilities.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = response.ContinuationToken;

                foreach (var item in response.Results)
                {
                    // item.AbilityId
                    // item.Picks
                    // item.Wins
                    // item.Kills
                }
            } while (continuationToken != null);
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
