using HGV.Basilius;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.AggregateHeroAbilities.Services
{
    public interface IAggregateHeroAbilitiesService
    {
        IEnumerable<int> GetAbilities();

        Task Process(HeroAggregateReference item, Dictionary<int, List<TextReader>> input, TextWriter writer);
    }

    public class AggregateHeroAbilitiesService : IAggregateHeroAbilitiesService
    {
        private readonly IEnumerable<int> _skills;

        public AggregateHeroAbilitiesService()
        {
            _skills = MetaClient.Instance.Value.GetSkills().Select(_ => _.Id);
        }

        public IEnumerable<int> GetAbilities()
        {
            return _skills;
        }

        public async Task Process(HeroAggregateReference item, Dictionary<int, List<TextReader>> input, TextWriter writer)
        {
            // Aggregate each ability into single document

            var collection = new List<HeroAbilityData>();
            foreach (var pair in input)
            {
                var summary = pair.Value
                    .Select(_ => _.ReadToEnd())
                    .Select(_ => JsonConvert.DeserializeObject<HeroAbilityData>(_))
                    .ToList();

                // Aggregate days into summary

                var data = new HeroAbilityData();
                data.Region = item.Region;
                data.HeroId = item.Hero;
                data.AbilityId = pair.Key;
                data.Date = item.Date7 + " - " + item.Date1;
                data.DraftOrder = summary.Sum(_ => _.DraftOrder);
                data.Losses = summary.Sum(_ => _.Losses);
                data.MaxAssists = summary.Sum(_ => _.MaxAssists);
                data.MaxGold = summary.Sum(_ => _.MaxGold);
                data.MaxKills = summary.Sum(_ => _.MaxKills);
                data.MinDeaths = summary.Sum(_ => _.MinDeaths);
                data.Total = summary.Sum(_ => _.Total);
                data.Wins = summary.Sum(_ => _.Wins);

                collection.Add(data);
            }

            var output = JsonConvert.SerializeObject(collection);
            await writer.WriteAsync(output);
        }
    }
}
