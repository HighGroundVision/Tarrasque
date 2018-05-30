
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using HGV.Tarrasque.Models;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnRefreshAccount
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("RefreshAccount")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            [Queue("hgv-refresh-account")]IAsyncCollector<AccountRefreshMessage> queue,
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

            var msg = new AccountRefreshMessage() { game_mode = gameMode, dota_id = accountId };
            await queue.AddAsync(msg);

            return new OkResult();
        }
    }
}
