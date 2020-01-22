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
        private const string CHECKPOINT_INSTANCE = "c2598345-6b54-43ee-81f1-90e0b936855f";
        private readonly IDotaService _service;

        public FnCheckpoint(IDotaService service)
        {
            _service = service;
        }

        [FunctionName("FnStartCheckpoint")]
        public async Task<IActionResult> StartCheckpoint(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/start")]HttpRequestMessage req,
           [DurableClient]IDurableClient fnClient,
           ILogger log)
        {
            var existing = await fnClient.GetStatusAsync(CHECKPOINT_INSTANCE);
            if(existing == null)
            {
                var data = _service.Initialize(log);
                await fnClient.StartNewAsync("FnRunCheckpoint", CHECKPOINT_INSTANCE, data);
            }
            else if (existing.RuntimeStatus == OrchestrationRuntimeStatus.Failed)
            {
                var data = existing.Input.ToObject<CheckpointModel>();
                await fnClient.StartNewAsync("FnRunCheckpoint", CHECKPOINT_INSTANCE, data);
            }

            var payload = fnClient.CreateHttpManagementPayload(CHECKPOINT_INSTANCE);
            return new RedirectResult(payload.StatusQueryGetUri, false);
        }

        [FunctionName("FnRunCheckpoint")]
        public async Task RunCheckpoint([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var input = context.GetInput<CheckpointModel>();
            var output = await context.CallActivityAsync<CheckpointModel>("FnProcessCheckpoint", input);

            DateTime delay = (output.Processed < 100) ? context.CurrentUtcDateTime.AddSeconds(30) : context.CurrentUtcDateTime.AddSeconds(1);
            await context.CreateTimer(delay, CancellationToken.None);

            context.ContinueAsNew(output);
        }

        [FunctionName("FnProcessCheckpoint")]
        public async Task<CheckpointModel> ProcessCheckpoint(
            [ActivityTrigger] CheckpointModel checkpoint,
            [DurableClient] IDurableEntityClient fnClient,
            [Queue("hgv-ad-matches")]IAsyncCollector<MatchRef> queue,
            ILogger log
        )
        {
            try
            {
                var collection = await _service.GetMatches(checkpoint, log);

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
            }
 
            return checkpoint;
        }
    }
}