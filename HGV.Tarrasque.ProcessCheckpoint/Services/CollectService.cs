using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Exceptions;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Services
{
    public interface ICollectService
    {
        Task<List<Match>> Collect(CheckpointModel checkpoint, ILogger log);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient apiClient;

        public CollectService(IDotaApiClient client)
        {
            this.apiClient = client;
        }

        public async Task<List<Match>> Collect(CheckpointModel checkpoint, ILogger log)
        {
            Guard.Argument(checkpoint, nameof(checkpoint)).NotNull();
            Guard.Argument(log, nameof(log)).NotNull();

            var matches = await GetMatches(checkpoint.Latest, log);

            var queue = matches.Where(_ => _.game_mode == 18).ToList();

            checkpoint.Total += queue.Count;
            checkpoint.Batch = matches.Count();
            checkpoint.Delta = (DateTimeOffset.UtcNow - matches.Max(_ => _.GetEnd())).ToString("c");
            checkpoint.Latest = matches.Max(_ => _.match_seq_num) + 1;

            return queue;
        }

        private async Task<List<Match>> GetMatches(ulong latest, ILogger log)
        {
            var policy = Policy
                .Handle<BelowLimitException>()
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(10),
                    (ex, timeout) => log.LogDebug(ex.Message)
                );

            var collection = await policy.ExecuteAsync<List<Match>>(async () => 
            {
                var matches = await this.apiClient.GetMatchesInSequence(latest);
                if (matches.Count < 100)
                    throw new BelowLimitException();
                else
                    return matches;
            });

            return collection;
        }
    }
}
