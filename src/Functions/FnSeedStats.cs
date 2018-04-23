using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnSeedStats
    {
        [FunctionName("FnSeedStats")]
        public static async Task Run(
            // Type
            [QueueTrigger("myqueue-items")]string type,
            // Stats Directory
            [Blob("hgv-stats", System.IO.FileAccess.ReadWrite)]CloudBlobContainer statsContainer,
            // Valid - abilities & ultimates
            [Blob("hgv-master/valid-abilities.json", System.IO.FileAccess.Read)]TextReader skillsBlob,
            [Blob("hgv-master/valid-ultimates.json", System.IO.FileAccess.Read)]TextReader ultimatesBlob,
            // Logger
            TraceWriter log)
        {
            log.Info($"FnSeedStats({type}): started at {DateTime.UtcNow}");

            var serailizer = JsonSerializer.CreateDefault();
            var skills = (List<int>)serailizer.Deserialize(skillsBlob, typeof(List<int>));
            var ultimates = (List<int>)serailizer.Deserialize(ultimatesBlob, typeof(List<int>));
            var json = JsonConvert.SerializeObject(new AbilitiesStats());

            if (type == "drafts")
            {
                // Create Quads
            }
            else if (type == "combos")
            {
                // Create Pairs
            }
            else if (type == "abilities")
            {
                var abilities = skills.Union(ultimates).ToList();
                foreach (var item in abilities)
                {
                    CloudBlockBlob blob = statsContainer.GetBlockBlobReference($"hgv-stats/drafts/{item}.json");
                    await blob.UploadTextAsync(json);
                }
            }
        }
    }
}
