using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.ProcessCheckpoint.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Functions
{
    public class FnCheckpoint
    {
        private readonly ICollectService collectionService;
        private readonly ICheckPointService checkpointService;

        public FnCheckpoint(ICollectService collectionService, ICheckPointService seedService)
        {
            this.collectionService = collectionService;
            this.checkpointService = seedService;
        }

        [FunctionName("FnCheckpoint")]
        public async Task Checkpoint(
            [BlobTrigger("hgv-checkpoint/master.json")]TextReader reader,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            [Queue("hgv-ad-regions")]IAsyncCollector<Match> qRegions,
            [Queue("hgv-ad-heroes")]IAsyncCollector<Match> qHeroes,
            [Queue("hgv-ad-abilities")]IAsyncCollector<Match> qAbilities,
            [Queue("hgv-ad-players")]IAsyncCollector<Match> qPlayers,
            ILogger log)
        {
            using (new Timer("FnCheckpoint", log))
            {
                var queues = new List<IAsyncCollector<Match>>() { qRegions, qHeroes, qAbilities, qPlayers };
                await this.collectionService.ProcessCheckpoint(reader, writer, queues, log);
            }
        }

        [FunctionName("FnCheckpointStart")]
        public async Task<IActionResult> CheckpointStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/start")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextReader reader,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            ILogger log)
        {
            using (new Timer("FnCheckpointStart", log))
            {
                var reset = req.Query.ContainsKey("reset");
                await this.checkpointService.StartCheckpoint(reader, writer, reset, log);
                return new OkResult();
            }
        }

        [FunctionName("FnCheckpointStatus")]
        public async Task<IActionResult> CheckpointStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/status")] HttpRequest req,
            [Blob("hgv-checkpoint/master.json")]TextReader reader,
            [Queue("hgv-ad-regions")]CloudQueue qRegions,
            [Queue("hgv-ad-heroes")]CloudQueue qHeroes,
            [Queue("hgv-ad-abilities")]CloudQueue qAbilities,
            [Queue("hgv-ad-players")]CloudQueue qPlayers,
            ILogger log)
        {
            using (new Timer("FnCheckpointStatus", log))
            {
                var queues = new Dictionary<string, CloudQueue>()
                {
                    { "Regions", qRegions},
                    { "Heroes", qHeroes},
                    { "Abilities", qAbilities},
                    { "Players", qPlayers},
                };
                var status = await this.checkpointService.CheckpointStatus(reader, queues, log);
                return new OkObjectResult(status);
            }
        }
    }
}
