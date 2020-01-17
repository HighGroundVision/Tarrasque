using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.API.Entities;
using HGV.Tarrasque.API.Models;
using HGV.Tarrasque.API.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Functions
{
    public class FnCounter
    {
        private readonly IDotaService _service;
        private readonly MetaClient _metaClient;

        public FnCounter(IDotaService service)
        {
            _service = service;
            _metaClient = MetaClient.Instance.Value;
        }

        [FunctionName("FnStartCounter")]
        public async Task StartCounter(
            [QueueTrigger("hgv-ad-matches")]MatchReference item,
            [DurableClient]IDurableClient fnClient,
            ILogger log)
        {
            await fnClient.StartNewAsync("FnRunCounter", item);
        }

        [FunctionName("FnRunCounter")]
        public async Task RunCounter([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var item = context.GetInput<MatchReference>();
            var match = await context.CallActivityAsync<Match>("FnFetchMatch", item);
            await context.CallActivityAsync("FnProcessRegions", match);
            await context.CallActivityAsync("FnProcessPlayers", match);
        }

        [FunctionName("FnFetchMatch")]
        public async Task<Match> FetchMatch(
            [ActivityTrigger] MatchReference item,
            ILogger log
        )
        {
            var match = await _service.GetMatch(item, log);
            return match;
        }

        [FunctionName("FnProcessRegions")]
        public async Task ProcessRegions(
            [ActivityTrigger] Match match,
            [DurableClient] IDurableEntityClient fnClient,
            ILogger log
        )
        {
            var key = _metaClient.GetRegionId(match.cluster).ToString();
            var entityId = new EntityId(nameof(RegionsCounter), key);
            await fnClient.SignalEntityAsync<IRegionsCounter>(entityId, proxy => proxy.Increment());
        }
        
        [FunctionName("FnProcessPlayers")]
        public async Task ProcessPlayers(
            [ActivityTrigger] Match match,
            [DurableClient] IDurableEntityClient fnClient,
            ILogger log
        )
        {
            const long CATCH_ALL_ACCOUNT = 4294967295;

            foreach (var player in match.players)
            {
                if (player.account_id == CATCH_ALL_ACCOUNT)
                    continue;

                var key = player.account_id.ToString();
                var entityId = new EntityId(nameof(PlayersCounter), key);

                var victory = (match.radiant_win && player.player_slot < 6);
                if (victory)
                    await fnClient.SignalEntityAsync<IPlayersCounter>(entityId, proxy => proxy.AddWin());
                else
                    await fnClient.SignalEntityAsync<IPlayersCounter>(entityId, proxy => proxy.AddLoss());

            }
        }

    }
}
