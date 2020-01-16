using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using HGV.Daedalus;
using HGV.Tarrasque.Api.Services;
using HGV.Tarrasque.Common.Models;

namespace HGV.Tarrasque.Api.Functions
{
    public class FnAPI
    {
        private readonly IDotaApiClient apiClient;
        private readonly ISeedService seedService;

        public FnAPI(IDotaApiClient client, ISeedService service)
        {
            this.apiClient = client;
            this.seedService = service;
        }

        [FunctionName("FnSeedCheckPoint")]
        public async Task<IActionResult> SeedCheckPoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "seed/checkpoint")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextWriter writerCheckpoint,
            ILogger log)
        {
            try
            {
                await this.seedService.SeedCheckpoint(writerCheckpoint);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                return new StatusCodeResult(500);
            }
        }

        [FunctionName("FnSeedHistory")]
        public async Task<IActionResult> SeedHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "seed/history")] HttpRequest req,
            [Blob("hgv-checkpoint/history.json", FileAccess.Write)]TextWriter writerHistory,
            ILogger log)
        {
            try
            {
                await this.seedService.SeedHistory(writerHistory);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                return new StatusCodeResult(500);
            }
        }

        [FunctionName("FnStart")]
        public async Task<IActionResult> StartCheckPoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "start")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextReader reader,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            ILogger log)
        {
            try
            {
                if (reader == null)
                    throw new ArgumentNullException(nameof(reader), "There is no Checkpoint");

                var json = await reader.ReadToEndAsync();
                await writer.WriteAsync(json);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                return new StatusCodeResult(500);
            }
        }

        [FunctionName("FnTriggerAggregates")]
        public async Task<IActionResult> TriggerAggregates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "aggregates/trigger")] HttpRequest req,
            [Queue("hgv-aggregates-trigger")]IAsyncCollector<AggregateTrigger> queue,
            ILogger log)
        {
            var item = new AggregateTrigger()
            {
                Timestamp = DateTime.UtcNow.Date,
            };

            await queue.AddAsync(item);

            return new OkResult();
        }

        [FunctionName("FnHeroAggregates")]
        public async Task<IActionResult> GetHeroAggregates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "aggregates/hero/{hero}/stats/{region}")] HttpRequest req,
            [Blob("hgv-summary-heroes/{region}/{hero}/stats.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }

        [FunctionName("FnHeroAbilitiesAggregates")]
        public async Task<IActionResult> GetHeroAbilitiesAggregates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "aggregates/hero/{hero}/abilities/{region}")] HttpRequest req,
            [Blob("hgv-summary-heroes/{region}/{hero}/abilities.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }

        [FunctionName("FnAbilityAggregates")]
        public async Task<IActionResult> GetAbilityAggregates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "aggregates/ability/{ability}/stats/{region}")] HttpRequest req,
            [Blob("hgv-summary-abilities/{region}/{ability}/stats.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }

        [FunctionName("FnPlayerAggregates")]
        public async Task<IActionResult> GetPlayerAggregates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "aggregates/player/{account}")] HttpRequest req,
            [Blob("hgv-accounts/{account}.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }

        [FunctionName("FnRegionCounts")]
        public async Task<IActionResult> GetRegionCounts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "daily/region/{region}/{date}")] HttpRequest req,
            [Blob("hgv-regions/{region}/{date}.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }

        [FunctionName("FnDailyHero")]
        public async Task<IActionResult> GetDailyHero(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "daily/hero/{region}/{date}/{hero}")] HttpRequest req,
            [Blob("hgv-heroes/{region}/{date}/{hero}.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }

        [FunctionName("FnDailyAbility")]
        public async Task<IActionResult> GetDailyAbility(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "daily/ability/{region}/{date}/{ability}")] HttpRequest req,
            [Blob("hgv-abilities/{region}/{date}/{ability}.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }
    }
}
