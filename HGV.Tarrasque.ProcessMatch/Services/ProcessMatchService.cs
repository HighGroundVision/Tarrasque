using Dawn;
using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HGV.Tarrasque.ProcessMatch.Entities;

namespace HGV.Tarrasque.ProcessMatch.Services
{
    public interface IProcessMatchService
    {
        Task ProcessMatch(MatchReference matchRef, IDurableEntityClient client);
    }

    public class ProcessMatchService : IProcessMatchService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public ProcessMatchService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = MetaClient.Instance.Value;
        }

        public async Task ProcessMatch(MatchReference matchRef, IDurableEntityClient client)
        {
            Guard.Argument(matchRef, nameof(matchRef)).NotNull().Member(_ => _.MatchId, _ => _.NotZero());
            Guard.Argument(client, nameof(client)).NotNull();

            var match = await FetchMatch(matchRef.MatchId);

            {
                var entityId = new EntityId(nameof(MatchCounter), match.game_mode.ToString());
                await client.SignalEntityAsync<ICounter>(entityId, proxy => proxy.Add(1));
            }

            foreach (var player in match.players)
            {
                var entityId = new EntityId(nameof(HeroCounter), player.hero_id.ToString());

                if (match.Victory(player))
                    await client.SignalEntityAsync<IHeroCounter>(entityId, proxy => proxy.AddWin());
                else
                    await client.SignalEntityAsync<IHeroCounter>(entityId, proxy => proxy.AddLoss());
            }
        }

        #region Match

        private async Task<Match> FetchMatch(ulong matchId)
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

        #endregion
    }
}
