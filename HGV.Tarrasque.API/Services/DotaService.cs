using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.API.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Services
{
    public interface IDotaService
    {
        Task<Checkpoint> Initialize(ILogger log);

        Task<List<Match>> GetMatches(Checkpoint checkpoint, ILogger log);
    }

    public class DotaService : IDotaService
    {
        private readonly IDotaApiClient _dotaClient;
        public DotaService(IDotaApiClient dotaClient)
        {
            _dotaClient = dotaClient;
        }

        public async Task<Checkpoint> Initialize(ILogger log)
        {
            Guard.Argument(log, nameof(log)).NotNull();

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
            Guard.Argument(checkpoint, nameof(checkpoint)).NotNull();
            Guard.Argument(log, nameof(log)).NotNull();

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

            if (matches.Count == 0)
                throw new ApplicationException("No Matches");

            return matches;
        }
    }
}
