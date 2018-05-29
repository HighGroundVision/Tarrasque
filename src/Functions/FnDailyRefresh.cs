using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace HGV.Tarrasque.Functions
{
    public static class FnDailyRefresh
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("DailyRefresh")]
        public static async Task Run(
            [TimerTrigger("0 0 * * *")]TimerInfo myTimer, 
            [Blob("hgv-master/leases.json", FileAccess.ReadWrite)]CloudBlockBlob blob, 
            [Queue("hgv-refresh-accounts")]IAsyncCollector<AccountRefreshMessage> queue, 
            TraceWriter log
        )
        {
            // Make sure the leases file exists
            var result = await blob.ExistsAsync();
            if (result == false)
            {
                var data = new List<AccountLeaseData>();
                var json = JsonConvert.SerializeObject(data);
                await blob.UploadTextAsync(json);
            }

            // Download the Lease data
            var jsonDownload = await blob.DownloadTextAsync();
            var collection = JsonConvert.DeserializeObject<List<AccountLeaseData>>(jsonDownload);

            // Remove any Lease that expired
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                if (collection[i].expiry < DateTime.UtcNow)
                    collection.RemoveAt(i);
            }

            // Upload new lease data
            var jsonUpload = JsonConvert.SerializeObject(collection);
            await blob.UploadTextAsync(jsonUpload);

            // Queue accounts to be refreshed
            foreach (var item in collection)
            {
                var msg = new AccountRefreshMessage() { game_mode = item.game_mode, dota_id = item.dota_id };
                await queue.AddAsync(msg);
            }
        }
    }
}
