using HGV.Daedalus;
using HGV.Tarrasque.API.Models;
using HGV.Tarrasque.API.Ulitities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Functions
{
    public static class FnCheckpoint
    {
        private static readonly IDotaApiClient dotaClient = new DotaApiClient(new SteamKeyProvider(), new SimplerHttpClientFactory());

        [FunctionName("FnRunCheckpoint")]
        public static async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var checkpoint = context.GetInput<Checkpoint>();

            var matches = await dotaClient.GetMatchesInSequence(checkpoint.Latest);
            checkpoint.Latest = matches.Max(_ => _.match_seq_num);

            DateTime next = context.CurrentUtcDateTime.AddSeconds(1);
            await context.CreateTimer(next, CancellationToken.None);

            context.ContinueAsNew(checkpoint);
        }

        [FunctionName("FnStartCheckpoint")]
        public static async Task<HttpResponseMessage> StartOrchestrator(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [DurableClient]IDurableClient fnClient,
            ILogger log)
        {
            var collection = await fnClient.GetStatusAsync();
            foreach (var status in collection)
            {
                if(status.Name == "FnRunCheckpoint")
                {
                    await fnClient.TerminateAsync(status.InstanceId, "There can be only one.");
                }
            }

            var matches = await dotaClient.GetLastestMatches();
            var data = new Checkpoint()
            {
                Latest = matches.Max(_ => _.match_seq_num),
            };

            string instanceId = await fnClient.StartNewAsync("FnRunCheckpoint", data);

            return fnClient.CreateCheckStatusResponse(req, instanceId);
        }
    }
}