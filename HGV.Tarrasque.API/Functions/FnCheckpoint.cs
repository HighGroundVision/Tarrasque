using DurableTask.Core;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.API.Entities;
using HGV.Tarrasque.API.Models;
using HGV.Tarrasque.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Functions
{
    public class FnCheckpoint
    {
        private readonly IDotaService _service;

        public FnCheckpoint(IDotaService service)
        {
            _service = service;
        }

        [FunctionName("FnCheckpointStart")]
        public async Task<IActionResult> Start(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/start")] HttpRequest req,
           [Blob("hgv-checkpoint/master.json")]TextWriter writer,
           ILogger log)
        {
            var checkpoint = await _service.Initialize(log);
            var json = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(json);

            return new RedirectResult("/api/checkpoint/status", false);
        }

        [FunctionName("FnCheckpointStatus")]
        public async Task<IActionResult> Status(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/status")] HttpRequest req,
           [Blob("hgv-checkpoint/master.json")]TextReader reader,
           ILogger log)
        {
            var json = await reader.ReadToEndAsync();
            return new OkObjectResult(json);
        }

        [FunctionName("FnCheckpointProcess")]
        public async Task Process(
            [BlobTrigger("hgv-checkpoint/master.json")]TextReader reader,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            [Queue("hgv-ad-matches")]IAsyncCollector<MatchRef> queue,
            [DurableClient] IDurableEntityClient fnClient,
            ILogger log
        )
        {
            var input = await reader.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<CheckpointModel>(input);

            try
            {
                var collection = await _service.GetMatches(checkpoint, log);
                if (collection.Count < 100)
                    throw new ApplicationException("Below Processing Limit");

                var modes = collection.GroupBy(_ => _.game_mode).Select(_ => new { Mode = _.Key, Matches = _.Count() }).ToList();
                foreach (var item in modes)
                {
                    var entityId = new EntityId(nameof(ModeEntity), item.Mode.ToString());
                    await fnClient.SignalEntityAsync<IModeEntity>(entityId, proxy => proxy.Add(item.Matches));
                }

                foreach (var item in collection)
                {
                    if (item.game_mode == 18)
                        await queue.AddAsync(new MatchRef() { Match = item });
                }

                var end_time = collection.Select(_ => _.start_time + _.duration).Max();
                checkpoint.Delta = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(end_time);
                checkpoint.Latest = collection.Max(_ => _.match_seq_num) + 1;
                checkpoint.Processed = collection.Count();
            }
            catch (Exception ex)
            {
                log.LogWarning(ex.Message);

                await Task.Delay(TimeSpan.FromSeconds(30));
            }

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);
        }
    }
}