using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.AggregateHero.Services
{
    public interface IAggregateHeroService
    {
        Task Process(HeroAggregateReference item, IReadOnlyList<TextReader> readers, TextWriter writer);
    }

    public class AggregateHeroService : IAggregateHeroService
    {
        public AggregateHeroService()
        {
        }

        public async Task Process(HeroAggregateReference heroRef, IReadOnlyList<TextReader> readers, TextWriter writer)
        {
            var data = new HeroData();
            data.Region = heroRef.Region;
            data.HeroId = heroRef.Hero;
            data.Date = heroRef.Date7 + " - " + heroRef.Date1;

            var collection = new List<HeroData>();
            foreach (var reader in readers)
            {
                var item = await GetData(reader);
                collection.Add(item);
            }

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

        private static async Task<HeroData> GetData(TextReader reader)
        {
            if (reader == null)
                return new HeroData();

            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(input))
                return new HeroData();

            return JsonConvert.DeserializeObject<HeroData>(input);
        }
    }
}
