using HGV.Daedalus;
using HGV.Tarrasque.Models;
using HGV.Tarrasque.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public class FnCheckpoint
    {
        private readonly IMatchConverterService converterService;
        private readonly IDotaApiClient apiClient;
        private readonly TimeSpan normalDelay;
        private readonly TimeSpan recoverDelay;

        public FnCheckpoint(IDotaApiClient apiClient, IMatchConverterService converterService)
        {
            this.converterService = converterService;
            this.apiClient = apiClient;
            this.recoverDelay = TimeSpan.FromSeconds(30);
            this.normalDelay = TimeSpan.FromSeconds(1);
        }

        [FunctionName("FnCheckpointStart")]
        public async Task<IActionResult> CheckpointStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkpoint/start")] HttpRequest req,
            [Queue("hgv-checkpoint")]IAsyncCollector<Checkpoint> queue,
            ILogger log)
        {
            var matches = await this.apiClient.GetLastestMatches();

            var checkpoint = new Checkpoint()
            {
                Lastest = matches.Max(_ => _.match_seq_num)
            };
            
            await queue.AddAsync(checkpoint);

            return new OkResult();
        }

        [FunctionName("FnCheckpointRun")]
        public async Task CheckpointRun(
            [QueueTrigger("hgv-checkpoint")]Checkpoint checkpoint,
            [Queue("hgv-checkpoint")]IAsyncCollector<Checkpoint> queue,
            [CosmosDB(
                databaseName: "Tarrasque", 
                collectionName: "Matches",
                ConnectionStringSetting = "CosmosDBConnection"
            )]IAsyncCollector<PlayerMatch> db,
            ILogger log
        )
        {
            try
            {
                // Get Matches
                var matches = await apiClient.GetMatchesInSequence(checkpoint.Lastest);

                // Filter to Ability Draft Matches
                var adMatches = matches.Where(_ => _.game_mode == 18).ToList();

                // Process Matches
                foreach (var match in adMatches)
                {
                    var documents = converterService.Convert(match);
                    foreach (var document in documents)
                    {
                        await db.AddAsync(document);
                    }
                }

                // Update Checkpoint
                checkpoint.Lastest = matches.Max(_ => _.match_seq_num);

                await Task.Delay(normalDelay);
            }
            catch(Exception ex)
            {
                // Log Error
                log.LogError(ex.Message);

                // Wait for API to recover
                await Task.Delay(recoverDelay);
            }
            finally
            {
                // Around and round we go again...
                await queue.AddAsync(checkpoint);
            }
        }
    }
}
