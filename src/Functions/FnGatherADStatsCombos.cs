using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace HGV.Tarrasque.Functions
{
    public static class FnGatherADStatsCombos
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("FnGatherADStatsCombos")]
        public static async Task Run(
            // Queue with {day}
            [QueueTrigger("hgv-ad-stats-combos")]string item,
            // Blob with matches / day
            [Blob("hgv-matches/{queueTrigger}/18")]CloudBlobDirectory directory,
            // AD Stats Tables
            [Table("hgv-ad-stats-combos")]CloudTable tableCombos,
            // Binder (dynamic output binding)
            Binder binder,
            // Logger
            TraceWriter log
        )
        {
            log.Info($"FnGatherADStatsCombos started at: {DateTime.Now}");

            var totalMatches = await CountMatches(directory);

            /*
            var attr = new BlobAttribute($"hgv-stats/18/abilities/{item}");
            using (var writer = await binder.BindAsync<TextWriter>(attr))
            {
            }
            */
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
