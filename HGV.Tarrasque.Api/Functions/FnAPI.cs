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
using HGV.Tarrasque.Common.Entities;

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

        [FunctionName("FnCheckpointStart")]
        public async Task<IActionResult> CheckpointStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/start")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextReader reader,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            ILogger log)
        {
            var reset = req.Query.ContainsKey("reset");
            if (reader == null || reset == true)
            {
                await this.seedService.SeedCheckpoint(writer);
            }
            else
            {
                var json = await reader.ReadToEndAsync();
                await writer.WriteAsync(json);
            }

            return new RedirectResult("/api/checkpoint/status", false);
        }

        [FunctionName("FnCheckpointStatus")]
        public async Task<IActionResult> CheckpointStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/status")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextReader reader,
            ILogger log)
        {
            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }

        [FunctionName("FnRegionCounts")]
        public IActionResult GetRegionCounts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "daily/region/{date}/{region}")] HttpRequest req,
            [Table("HGVRegions", "{date}", "{region}")]RegionEntity entity,
            ILogger log)
        {
            if (entity == null)
                return new NotFoundResult();

            var json = JsonConvert.SerializeObject(entity);
            return new OkObjectResult(json);
        }
    }
}
