using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Collection.Extensions;
using HGV.Tarrasque.Collection.Models;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Collection.Services
{
    public interface ICollectService
    {    
        Task Collect(TextReader reader, TextWriter writer, IAsyncCollector<MatchReference> queue);
        Task CopyCheckpointForward(TextReader reader, TextWriter writer);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public CollectService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = new MetaClient();
        }

        public async Task Collect(TextReader reader, TextWriter writer, IAsyncCollector<MatchReference> queue)
        {
            var input = await reader.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<Models.Checkpoint>(input);

            var matches = await TryGetMatches(checkpoint.Latest);

            var collection = matches
                .Where(_ => _.game_mode == 18)
                .Where(_ => _.GetDuration().TotalMinutes > 15)
                .ToList();

            checkpoint.Split = DateTimeOffset.UtcNow - matches.Max(_ => _.GetEnd());
            checkpoint.Latest = matches.Max(_ => _.match_seq_num);
            checkpoint.TotalMatches += matches.Count();
            checkpoint.TotalADMatches += collection.Count();

            foreach (var item in collection)
            {
                if (checkpoint.History.Contains(item.match_id) == false)
                {
                    checkpoint.AddHistory(item.match_id);
                    
                    var obj = new MatchReference() { 
                        Match = item.match_id, 
                        Date = item.GetStart().ToString("yy-MM-dd"),
                        Region = this.metaClient.ConvertClusterToRegion(item.cluster)
                    };
                    await queue.AddAsync(obj);
                }
            }

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);
        }

        public async Task CopyCheckpointForward(TextReader reader, TextWriter writer)
        {
            var json = await reader.ReadToEndAsync();
            await writer.WriteAsync(json);
        }

        private async Task<List<Match>> TryGetMatches(long latest)
        {
            // NOTE: Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
            var waitAndRetryPolicy = Policy
               .Handle<Exception>(e => !(e is BrokenCircuitException))
               .WaitAndRetryForeverAsync(attempt => TimeSpan.FromMilliseconds(200));

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            var policy = Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicy);

            var collection = await policy.ExecuteAsync<List<Match>>(async () =>
            {
                var matches = await this.apiClient.GetMatchesInSequence(latest);

                if (matches.Count == 0)
                    throw new ApplicationException("No Matches Returned from API");

                return matches;
            });

            return collection;
        }
    }
}
