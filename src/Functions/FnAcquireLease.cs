
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System.Collections.Generic;
using HGV.Tarrasque.Models;
using System.Linq;
using System;

namespace HGV.Tarrasque.Functions
{
    public static class FnAcquireLease
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("AcquireLease")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            [Blob("hgv-master/leases.json")]CloudBlockBlob blob,
            TraceWriter log)
        {
            string accountQuery = req.Query["account"].ToString();
            if (string.IsNullOrWhiteSpace(accountQuery))
                return new BadRequestObjectResult("Please pass an [account] id on the query string");

            string modeQuery = req.Query["mode"].ToString();
            if (string.IsNullOrWhiteSpace(modeQuery))
                return new BadRequestObjectResult("Please pass a game [mode] on the query string");

            var accountId = long.Parse(accountQuery);
            var gameMode = int.Parse(modeQuery);

            var result = await blob.ExistsAsync();
            if (result == false)
            {
                var data = new List<AccountLeaseData>();
                var json = JsonConvert.SerializeObject(data);
                await blob.UploadTextAsync(json);
            }

            var jsonDownload = await blob.DownloadTextAsync();
            var collection = JsonConvert.DeserializeObject<List<AccountLeaseData>>(jsonDownload);

            var item = collection.FirstOrDefault(_ => _.dota_id == accountId && _.game_mode == gameMode);
            if(item == null)
            {
                var data = new AccountLeaseData() { dota_id = accountId, game_mode = gameMode, expiry = DateTime.UtcNow.Date.AddDays(7) };
                collection.Add(data);
                item = data;
            }

            var jsonUpload = JsonConvert.SerializeObject(collection);
            await blob.UploadTextAsync(jsonUpload);

            return new OkObjectResult(item);
        }
    }
}
