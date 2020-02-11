using Dawn;
using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessMatch.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Services
{
    public interface IProcessMatchService
    {
        Task ProcessMatch(Match match, IDurableEntityClient client, ILogger log);
    }

    public class ProcessMatchService : IProcessMatchService
    {
        public async Task ProcessMatch(Match match, IDurableEntityClient client, ILogger log)
        {
            {
                var region = match.GetRegion();
                var id = new EntityId(nameof(RegionEntity), region.ToString());
                await client.SignalEntityAsync<IRegionEntity>(id, _ => _.IncrementTotal());
            }

            foreach (var player in match.players)
            {
                var hero = player.GetHero();
                var victory = match.Victory(player);
                var priority = player.PickPriority();

                {
                    var id = new EntityId(nameof(HeroEntity), $"{hero.Id}");
                    await client.SignalEntityAsync<IHeroEntity>(id, _ => _.IncrementTotal());
                    if (victory) await client.SignalEntityAsync<IHeroEntity>(id, _ => _.IncrementWins());
                }

                var skills = player.GetSkills();
                foreach (var ability in skills)
                {
                    {
                        var id = new EntityId(nameof(AbilityEntity), $"{ability.Id}");
                        await client.SignalEntityAsync<IAbilityEntity>(id, _ => _.IncrementTotal());
                        await client.SignalEntityAsync<IAbilityEntity>(id, _ => _.AddPriority(priority));
                        if (victory) await client.SignalEntityAsync<IAbilityEntity>(id, _ => _.IncrementWins());
                        if (player.hero_id == ability.HeroId) await client.SignalEntityAsync<IAbilityEntity>(id, _ => _.IncrementAncestry());
                    }
                    {
                        var id = new EntityId(nameof(HeroComboEntity), $"{hero.Id}-{ability.Id}");
                        await client.SignalEntityAsync<IHeroComboEntity>(id, _ => _.IncrementTotal());
                        if (victory) await client.SignalEntityAsync<IHeroComboEntity>(id, _ => _.IncrementWins());
                    }
                }

                var pairs = player.GetPairs();
                foreach (var pair in pairs)
                {
                    var collection = new List<EntityId>()
                    {
                        new EntityId(nameof(AbilityComboEntity), $"{pair.Item1.Id}-{pair.Item2.Id}"),
                        new EntityId(nameof(AbilityComboEntity), $"{pair.Item2.Id}-{pair.Item1.Id}")
                    };
                    foreach (var id in collection)
                    {
                        await client.SignalEntityAsync<IAbilityComboEntity>(id, _ => _.IncrementTotal());
                        if (victory) await client.SignalEntityAsync<IAbilityComboEntity>(id, _ => _.IncrementWins());
                    }
                }

                var talents = player.GetTalenets();
                foreach (var talent in talents)
                {
                    var id = new EntityId(nameof(TalenentEntity), $"{talent.Id}");
                    await client.SignalEntityAsync<ITalenentEntity>(id, _ => _.IncrementTotal());
                    if (victory) await client.SignalEntityAsync<ITalenentEntity>(id, _ => _.IncrementWins());
                }
            }
        }

        // var attr = new BlobAttribute($"hgv-abilities/{timestamp}/{ability.Id}.json");
        // var attr = new BlobAttribute($"hgv-hero-combos/{timestamp}/{hero.Id}/{ability.Id}.json");
        // var attr = new BlobAttribute($"hgv-talenets/{timestamp}/{talent.Id}.json");
        // var attr = new BlobAttribute($"hgv-ability-combos/{timestamp}/{primaryAbility.Id}/{comboAbility.Id}.json");
    }
}
