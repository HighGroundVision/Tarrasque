using HGV.Basilius;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.AggregatesTrigger.Services
{
    public interface ITriggerAggregatesService
    {
        Task QueueRegions(IAsyncCollector<RegionAggregateReference> queue);
        Task QueueHeroes(IAsyncCollector<HeroAggregateReference> queue);
        Task QueueHeroAbilities(IAsyncCollector<HeroAggregateReference> queue);
        Task QueueAbilities(IAsyncCollector<AbilityAggregateReference> queue);
    }

    public class TriggerAggregatesService : ITriggerAggregatesService
    {
        private readonly List<int> _regions;
        private readonly List<int> _heroes;
        private readonly List<int> _skills;

        public TriggerAggregatesService()
        {
            var client = MetaClient.Instance.Value;
            _regions = client.GetRegions().Select(_ => _.Key).ToList();
            _heroes = client.GetHeroes().Select(_ => _.Id).ToList();
            _skills = client.GetSkills().Select(_ => _.Id).ToList();
        }

        public async Task QueueRegions(IAsyncCollector<RegionAggregateReference> queue)
        {
            var timestamp = DateTime.Today;

            var item = new RegionAggregateReference();
            item.Range.Add(timestamp.AddDays(-1).ToString("yy-MM-dd"));
            item.Range.Add(timestamp.AddDays(-2).ToString("yy-MM-dd"));
            item.Range.Add(timestamp.AddDays(-3).ToString("yy-MM-dd"));
            item.Range.Add(timestamp.AddDays(-4).ToString("yy-MM-dd"));
            item.Range.Add(timestamp.AddDays(-5).ToString("yy-MM-dd"));
            item.Range.Add(timestamp.AddDays(-6).ToString("yy-MM-dd"));
            item.Range.Add(timestamp.AddDays(-7).ToString("yy-MM-dd"));

            await queue.AddAsync(item);
        }

        public async Task QueueHeroes(IAsyncCollector<HeroAggregateReference> queue)
        {
            foreach (var regionId in _regions)
            {
                foreach (var heroId in _heroes)
                {
                    var item = new HeroAggregateReference();
                    item.Region = regionId;
                    item.Hero = heroId;
                    SetDates(item);

                    await queue.AddAsync(item);
                }
            }
        }

        public async Task QueueHeroAbilities(IAsyncCollector<HeroAggregateReference> queue)
        {
            foreach (var regionId in _regions)
            {
                foreach (var heroId in _heroes)
                {
                    var item = new HeroAggregateReference();
                    item.Region = regionId;
                    item.Hero = heroId;
                    SetDates(item);

                    await queue.AddAsync(item);
                }
            }
        }

        public async Task QueueAbilities(IAsyncCollector<AbilityAggregateReference> queue)
        {
            foreach (var regionId in _regions)
            {
                foreach (var abilityId in _skills)
                {
                    var item = new AbilityAggregateReference();
                    item.Region = regionId;
                    item.Ability = abilityId;
                    SetDates(item);

                    await queue.AddAsync(item);
                }
            }
        }

        private static void SetDates(AggregateReference item)
        {
            var timestamp = DateTime.Today;
            item.Date1 = timestamp.AddDays(-1).ToString("yy-MM-dd");
            item.Date2 = timestamp.AddDays(-2).ToString("yy-MM-dd");
            item.Date3 = timestamp.AddDays(-3).ToString("yy-MM-dd");
            item.Date4 = timestamp.AddDays(-4).ToString("yy-MM-dd");
            item.Date5 = timestamp.AddDays(-5).ToString("yy-MM-dd");
            item.Date6 = timestamp.AddDays(-6).ToString("yy-MM-dd");
            item.Date7 = timestamp.AddDays(-7).ToString("yy-MM-dd");
        }
    }
}
