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


        [FunctionName("FnTimeline")]
        public async Task<IActionResult> GetTimeline(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timelime/{id:int}")] HttpRequest req,
            [Blob("hgv-regions/{id}.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            return new OkObjectResult(
                await reader.ReadToEndAsync()
            );
        }

        [FunctionName("FnHeroes")]
        public async Task<IActionResult> GetHeroes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "heroes/{region}/{date}")] HttpRequest req,
            [Blob("hgv-heroes/{region}/{date}/heroes.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            return new OkObjectResult(
                await reader.ReadToEndAsync()
            );
        }

        [FunctionName("FnAbilities")]
        public async Task<IActionResult> GetAbilities(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "abilities/{region}/{date}")] HttpRequest req,
            [Blob("hgv-abilities/{region}/{date}/abilities.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            return new OkObjectResult(
                await reader.ReadToEndAsync()
            );
        }

        [FunctionName("FnPlayer")]
        public async Task<IActionResult> GetPlayer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/{account}")] HttpRequest req,
            [Blob("hgv-accounts/{account}.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            return new OkObjectResult(
                await reader.ReadToEndAsync()
            );
        }
    }
}
