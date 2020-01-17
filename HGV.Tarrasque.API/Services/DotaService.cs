using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.API.Models;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Services
{
    public interface IDotaService
    {
        Task<Checkpoint> InitializeCheckpoint(ILogger log);

        Task<List<Match>> GetMatches(Checkpoint checkpoint, ILogger log);

        Task<Match> GetMatch(MatchReference item, ILogger log);
    }

    public class DotaService : IDotaService
    {
        private readonly IDotaApiClient _dotaClient;

        public DotaService(IDotaApiClient dotaClient)
        {
            _dotaClient = dotaClient;
        }

        public async Task<Checkpoint> InitializeCheckpoint(ILogger log)
        {
            var policy = Policy
                 .Handle<Exception>()
                 .WaitAndRetryAsync(3,
                    (n) => TimeSpan.FromSeconds(30),
                    (ex, span) => log.LogWarning(ex.Message)
                );

            var latest = await policy.ExecuteAsync(async () =>
            {
                var matches = await _dotaClient.GetLastestMatches();
                return matches.Max(_ => _.match_seq_num);
            });

            return new Checkpoint() { Latest = latest };
        }

        public async Task<List<Match>> GetMatches(Checkpoint checkpoint, ILogger log)
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(6, 
                    (n) => TimeSpan.FromSeconds(30), 
                    (ex, span) => log.LogWarning(ex.Message)
                );

            var matches = await policy.ExecuteAsync(async () =>
            {
                return await _dotaClient.GetMatchesInSequence(checkpoint.Latest);
            });

            return matches;
        }

        public async Task<Match> GetMatch(MatchReference item, ILogger log)
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(6,
                    (n) => TimeSpan.FromSeconds(30),
                    (ex, span) => log.LogWarning(ex.Message)
                );

            var match = await policy.ExecuteAsync(async () =>
            {
                return await _dotaClient.GetMatchDetails(item.MatchId);
            });

            return match;
        }
    }
}
