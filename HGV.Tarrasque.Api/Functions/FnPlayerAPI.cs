
using HGV.Basilius;
using HGV.Daedalus;
using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Api.Services;
using HGV.Tarrasque.Common.Entities;
using HGV.Tarrasque.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Functions
{
    public class FnPlayerAPI
    {
        private readonly IReadOnlyPolicyRegistry<string> policyRegistry;
        private readonly IPlayerService playerService;

        public FnPlayerAPI(IReadOnlyPolicyRegistry<string> policyRegistry, IPlayerService playerService)
        {
            this.policyRegistry = policyRegistry;
            this.playerService = playerService;
        }

        [FunctionName("FnRegisterPlayer")]
        public async Task<IActionResult> RegisterPlayer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "player/register/{account}")] HttpRequest req,
            string account,
            IBinder binder,
            ILogger log)
        {
            var accountId = long.Parse(account);
            await this.playerService.RegisterPlayer(accountId, binder, log);

            return new OkResult();
        }

        [FunctionName("FnLeaderboardGlobal")]
        public async Task<IActionResult> GetLeaderboardGlobal(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/leaderboard")] HttpRequest req,
            [Table("HGVPlayers")]CloudTable table,
            ILogger log)
        {
            var cachePolicy = policyRegistry.Get<IAsyncPolicy<List<PlayerModel>>>("FnLeaderboardGlobal");
            var collection = await cachePolicy.ExecuteAsync(
               context => playerService.GetGlobalLeaderboard(table, log),
               new Context("FnLeaderboardGlobal")
            );

            return new OkObjectResult(collection);
        }

        [FunctionName("FnLeaderboardByRegion")]
        public async Task<IActionResult> GetLeaderboardByRegion(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/leaderboard/{region}")] HttpRequest req,
            int region,
            [Table("HGVPlayers")]CloudTable table,
            ILogger log)
        {
            var context = new Context("FnLeaderboardByRegion");
            context["region"] = region.ToString();
            var cachePolicy = policyRegistry.Get<IAsyncPolicy<List<PlayerModel>>>("FnLeaderboardByRegion");
            var collection = await cachePolicy.ExecuteAsync(
               context => playerService.GetRegionalLeaderboard(region, table, log),
               context
            );

            return new OkObjectResult(collection);
        }

    }
}
