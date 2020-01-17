using HGV.Tarrasque.API.Entities;
using HGV.Tarrasque.API.Models;
using HGV.Tarrasque.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        [FunctionName("FnRunCheckpoint")]
        public async Task RunCheckpoint([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var checkpoint = context.GetInput<Checkpoint>();

            await context.CallActivityAsync("FnProcessCheckpoint", checkpoint);

            DateTime next = context.CurrentUtcDateTime.AddSeconds(1);
            await context.CreateTimer(next, CancellationToken.None);

            context.ContinueAsNew(checkpoint);
        }

        [FunctionName("FnProcessCheckpoint")]
        public async Task ProcessCheckpoint(
            [ActivityTrigger] Checkpoint checkpoint,
            [DurableClient] IDurableEntityClient fnClient,
            [Queue("hgv-ad-matches")]IAsyncCollector<MatchReference> queue,
            ILogger log
        )
        {
            var collection = await _service.GetMatches(checkpoint, log);

            var modes = collection.GroupBy(_ => _.game_mode).Select(_ => new { Mode = _.Key, Matches = _.Count() }).ToList();
            foreach (var item in modes)
            {
                var entityId = new EntityId(nameof(MatchCounter), item.Mode.ToString());
                await fnClient.SignalEntityAsync<IMatchesCounter>(entityId, proxy => proxy.Add(item.Matches));
            }

            foreach (var item in collection)
            {
                if(item.game_mode == 18)
                {
                    await queue.AddAsync(new MatchReference() { MatchId = item.match_id });
                }
            }
        }

        [FunctionName("FnStartCheckpoint")]
        public async Task<HttpResponseMessage> StartCheckpoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/start")]HttpRequestMessage req,
            [DurableClient]IDurableClient fnClient,
            ILogger log)
        {
            var condision = new OrchestrationStatusQueryCondition()
            {
                RuntimeStatus = new List<OrchestrationRuntimeStatus>() { OrchestrationRuntimeStatus.Pending, OrchestrationRuntimeStatus.Running }
            };
            var collection = await fnClient.GetStatusAsync(condision, CancellationToken.None);
            foreach (var status in collection.DurableOrchestrationState)
            {
                if(status.Name == "FnRunCheckpoint")
                {
                    await fnClient.TerminateAsync(status.InstanceId, "There can be only one.");
                }
            }

            var data = await _service.InitializeCheckpoint(log);
            string instanceId = await fnClient.StartNewAsync("FnRunCheckpoint", data);

            return fnClient.CreateCheckStatusResponse(req, instanceId);
        } 
    }
}