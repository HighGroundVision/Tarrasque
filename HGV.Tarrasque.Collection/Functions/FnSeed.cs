using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using HGV.Tarrasque.Collection.Services;

namespace HGV.Tarrasque.Collection.Functions
{
    public class FnSeed
    {
        private readonly ISeedService _service;

        public FnSeed(ISeedService service)
        {
            _service = service;
        }

        [FunctionName("FnSeedCheckPoint")]
        public async Task<IActionResult> SeedCheckPoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "seed/checkpoint")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextWriter writerCheckpoint,
            ILogger log)
        {
            try
            {
                await _service.SeedCheckpoint(writerCheckpoint);

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
                await _service.SeedHistory(writerHistory);

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

        [FunctionName("FnTest")]
        public async Task<IActionResult> Test(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "regions/{id:int}")] HttpRequest req,
           int id,
           [Blob("hgv-regions/{id}.json")]TextReader reader,
           ILogger log)
        {
            try
            {
                var json = await reader.ReadToEndAsync();
                return new OkObjectResult(json);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                return new StatusCodeResult(500);
            }

        }
    }
}
