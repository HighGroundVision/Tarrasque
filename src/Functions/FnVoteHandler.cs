using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace HGV.Tarrasque.Functions
{
    public class RankingData
    {
        public string key { get; set; }
        public int up { get; set; }
        public int down { get; set; }
        public int total { get; set; }
    }

    public static class FnVoteHandler
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("VoteHandler")]
        public static async Task Run(
            [QueueTrigger("hgv-vote")]VoteData msg,
            [Blob("hgv-votes/abilities.json", FileAccess.ReadWrite)]CloudBlockBlob blobAbilities,
            [Blob("hgv-votes/combos.json", FileAccess.ReadWrite)]CloudBlockBlob blobCombos,
            TraceWriter log
        )
        {
            var downloadJson = String.Empty;
            if(msg.type == 1)
            {
                downloadJson = await blobAbilities.DownloadTextAsync();
            }
            else if (msg.type == 2)
            {
                downloadJson = await blobCombos.DownloadTextAsync();
            }

            // Get Collection
            var collection = JsonConvert.DeserializeObject<Dictionary<string, RankingData>>(downloadJson);
            if(collection.ContainsKey(msg.key) == false)
            {
                collection.Add(msg.key, new RankingData() { key = msg.key });
            }

            // Get Item
            var item = collection[msg.key];

            // Update vote
            if (msg.vote == true)
            {
                item.up++;
            }
            else
            {
                item.down++;
            }

            // Recalucate Total
            item.total = item.up - item.down;
            item.total = Math.Max(item.total, 0);

            var uploadJson = JsonConvert.SerializeObject(collection);

            if (msg.type == 1)
            {
                await blobAbilities.UploadTextAsync(uploadJson);
            }
            else if (msg.type == 2)
            {
                await blobCombos.UploadTextAsync(uploadJson);
            }
        }
    }
}
