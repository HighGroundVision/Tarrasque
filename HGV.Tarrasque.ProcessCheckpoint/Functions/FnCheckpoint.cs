using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.ProcessCheckpoint.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Functions
{
    public class FnCheckpoint
    {
        private readonly ICollectService collectionService;
        private readonly ISeedService seedService;

        public FnCheckpoint(ICollectService collectionService, ISeedService seedService)
        {
            this.collectionService = collectionService;
            this.seedService = seedService;
        }

        [FunctionName("FnCheckpoint")]
        public async Task Checkpoint(
            [BlobTrigger("hgv-checkpoint/master.json")]TextReader reader,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            [Queue("hgv-ad-matches")]CloudQueue queue,
            [DurableClient]IDurableClient client,
            ILogger log)
        {
            using (new Timer("FnCheckpoint", log))
            {
                await collectionService.ProcessCheckpoint(reader, writer, queue, client, log);
            }
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
                await seedService.SeedCheckpoint(writer);
            }
            else
            {
                var json = await reader.ReadToEndAsync();
                await writer.WriteAsync(json);
            }

            return new OkResult();
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
    }
}
