using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.AggregateAbility.Services
{
    public interface IAggregateAbilityService
    {
        Task Process(AbilityAggregateReference item, IReadOnlyList<TextReader> readers, TextWriter writer);
    }

    public class AggregateAbilityService : IAggregateAbilityService
    {
        public AggregateAbilityService()
        {
        }

        public async Task Process(AbilityAggregateReference abilityRef, IReadOnlyList<TextReader> readers, TextWriter writer)
        {
            var data = new AbilityData();
            data.Region = abilityRef.Region;
            data.AbilityId = abilityRef.Ability;
            data.Date = abilityRef.Date7 + " - " + abilityRef.Date1;

            var collection = new List<AbilityData>();
            foreach (var reader in readers)
            {
                var item = await GetData(reader);
                collection.Add(item);
            }

            data.HeroAbility = collection.Sum(_ => _.HeroAbility);
            data.DraftOrder = collection.Sum(_ => _.DraftOrder);
            data.Losses = collection.Sum(_ => _.Losses);
            data.MaxAssists = collection.Sum(_ => _.MaxAssists);
            data.MaxGold = collection.Sum(_ => _.MaxGold);
            data.MaxKills = collection.Sum(_ => _.MaxKills);
            data.MinDeaths = collection.Sum(_ => _.MinDeaths);
            data.Total = collection.Sum(_ => _.Total);
            data.Wins = collection.Sum(_ => _.Wins);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static async Task<AbilityData> GetData(TextReader reader)
        {
            if (reader == null)
                return new AbilityData();

            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(input))
                return new AbilityData();

            return JsonConvert.DeserializeObject<AbilityData>(input);
        }
    }
}
