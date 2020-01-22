using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.API.Entities;
using HGV.Tarrasque.API.Models;
using HGV.Tarrasque.API.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace HGV.Tarrasque.API.Functions
{
    public class FnProcessMatch
    {
        private readonly IDotaService _service;
        private readonly MetaClient _metaClient;
        private const long CATCH_ALL_ACCOUNT = 4294967295;

        public FnProcessMatch(IDotaService service)
        {
            _service = service;
            _metaClient = MetaClient.Instance.Value;
        }

        [FunctionName("FnProcessMatch")]
        public async Task Process(
            [QueueTrigger("hgv-ad-matches")]MatchRef item,
            [Queue("hgv-ad-players")]IAsyncCollector<AccountRef> queue,
            [DurableClient]IDurableClient fnClient,
            ILogger log)
        {
            var match = item.Match;

            await UpdateRegion(match, fnClient);

            foreach (var player in match.players)
            {
                await QueueAccount(queue, match, player);

                var victory = (match.radiant_win && player.player_slot < 6);

                await UpdateHero(fnClient, player.hero_id, victory);

                var talents = player.GetTalents();
                await UpdateTalents(fnClient, player.hero_id, talents, victory);

                var abilities = player.GetAbilities();
                await UpdateHeroPair(fnClient, player.hero_id, abilities, victory);
                await UpdateAbilities(fnClient, abilities, victory);
                await UpdateAbilityPairs(fnClient, abilities, victory);
            }
        }

        private async Task QueueAccount(IAsyncCollector<AccountRef> queue, Match match, Player player)
        {
            if (player.account_id == CATCH_ALL_ACCOUNT)
                return;

            await queue.AddAsync(new AccountRef() { AccountId = player.account_id, Player = player, Match = match });
        }

        private async Task UpdateRegion(Match match, IDurableClient fnClient)
        {
            var key = _metaClient.GetRegionId(match.cluster).ToString();
            var entityId = new EntityId(nameof(RegionEntity), key);
            await fnClient.SignalEntityAsync<IRegionEntity>(entityId, proxy => proxy.Increment());
        }

        private static async Task UpdateHero(IDurableClient fnClient, int id, bool victory)
        {
            var entityId = new EntityId(nameof(HeroEntity), id.ToString());
            await fnClient.SignalEntityAsync<IHeroEntity>(entityId, proxy => proxy.Increment(victory));
        }

        private static async Task UpdateHeroPair(IDurableClient fnClient, int hero_id, List<int> abilities, bool victory)
        {
            var entityId = new EntityId(nameof(HeroPairEntity), hero_id.ToString());
            await fnClient.SignalEntityAsync<IHeroPairEntity>(entityId, proxy => proxy.Increment(abilities, victory));
        }

        private static async Task UpdateAbilities(IDurableClient fnClient, List<int> abilities, bool victory)
        {
            foreach (var id in abilities)
            {
                var entityId = new EntityId(nameof(AbilityEntity), id.ToString());
                await fnClient.SignalEntityAsync<IAbilityEntity>(entityId, proxy => proxy.Increment(victory));
            }
        }

        private static async Task UpdateAbilityPairs(IDurableClient fnClient, List<int> abilities, bool victory)
        {
            foreach (var id in abilities)
            {
                var filtered = abilities.Where(_ => _ != id).ToList();

                var entityId = new EntityId(nameof(AbilityPairEntity), id.ToString());
                await fnClient.SignalEntityAsync<IAbilityPairEntity>(entityId, proxy => proxy.Increment(filtered, victory));
            }
            
        }

        private static async Task UpdateTalents(IDurableClient fnClient, int hero_id, List<int> talents, bool victory)
        {
            var entityId = new EntityId(nameof(TalentEntity), hero_id.ToString());
            await fnClient.SignalEntityAsync<ITalentEntity>(entityId, proxy => proxy.Increment(talents, victory));
        }
    }
}

