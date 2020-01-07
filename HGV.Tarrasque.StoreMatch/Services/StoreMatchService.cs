using Dawn;
using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Polly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.StoreMatch.Services
{
    public interface IStoreMatchService
    {
        Task<Match> FetchMatch(long match);

        Task StoreMatch(Match match, TextWriter writer);

    }

    public class StoreMatchService : IStoreMatchService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public StoreMatchService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = MetaClient.Instance.Value;
        }

        public async Task<Match> FetchMatch(long matchId)
        {
            Guard.Argument(matchId, nameof(matchId)).Positive().NotZero();

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                });

            var match = await policy.ExecuteAsync<Match>(async () =>
            {
                var details = await this.apiClient.GetMatchDetails(matchId);
                return details;
            });

            return match;
        }

        public async Task StoreMatch(Match match, TextWriter writer)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var json = JsonConvert.SerializeObject(match);
            await writer.WriteAsync(json);
        }


    }
}
