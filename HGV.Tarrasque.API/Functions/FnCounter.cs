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
        private const long CATCH_ALL_ACCOUNT = 4294967295;

        public FnCounter(IDotaService service)
        {
            _service = service;
            _metaClient = MetaClient.Instance.Value;
        }

        [FunctionName("FnProcessMatch")]
        public async Task ProcessMatch(
            [QueueTrigger("hgv-ad-matches")]Match match,
            [DurableClient]IDurableClient fnClient,
            //[Queue("hgv-ad-accounts")]IAsyncCollector<Match> queue,
            ILogger log)
        {
            {
                var key = _metaClient.GetRegionId(match.cluster).ToString();
                var entityId = new EntityId(nameof(RegionsCounter), key);
                await fnClient.SignalEntityAsync<IRegionsCounter>(entityId, proxy => proxy.Increment());
            }

            foreach (var player in match.players)
            {
                var victory = (match.radiant_win && player.player_slot < 6);

                /*
                if (player.account_id != CATCH_ALL_ACCOUNT)
                {
                    var entityId = new EntityId(nameof(PlayersCounter), player.account_id.ToString());
                    if (victory)
                        await fnClient.SignalEntityAsync<IPlayersCounter>(entityId, proxy => proxy.AddWin());
                    else
                        await fnClient.SignalEntityAsync<IPlayersCounter>(entityId, proxy => proxy.AddLoss());
                }
                */

                {
                    var entityId = new EntityId(nameof(HeroesCounter), player.hero_id.ToString());
                    if (victory)
                        await fnClient.SignalEntityAsync<IHeroesCounter>(entityId, proxy => proxy.AddWin());
                    else
                        await fnClient.SignalEntityAsync<IHeroesCounter>(entityId, proxy => proxy.AddLoss());
                }

            }
        }
    }
}
