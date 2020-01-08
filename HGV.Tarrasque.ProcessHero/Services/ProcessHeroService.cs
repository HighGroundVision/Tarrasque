using Dawn;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessHero.Services
{
    public interface IProcessHeroService
    {
        Task ProcessHero(HeroReference heroRef, TextReader reader, TextWriter writer);
    }

    public class ProcessHeroService : IProcessHeroService
    {
        public ProcessHeroService()
        {
        }

        public async Task ProcessHero(HeroReference heroRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(heroRef, nameof(heroRef)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            if (reader == null)
                await NewHero(heroRef, writer);
            else
                await UpdateHero(heroRef, reader, writer);
        }


        private static async Task NewHero(HeroReference heroRef, TextWriter writer)
        {
            Guard.Argument(heroRef, nameof(heroRef)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var data = new HeroData();
            data.Region = heroRef.Region;
            data.Date = heroRef.Date;
            data.HeroId = heroRef.Hero;

            SetHeroData(heroRef, data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static async Task UpdateHero(HeroReference heroRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(heroRef, nameof(heroRef)).NotNull();
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var input = await reader.ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<HeroData>(input);
            
            SetHeroData(heroRef, data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static void SetHeroData(HeroReference heroRef, HeroData data)
        {
            data.Total++;
            data.Wins += heroRef.Wins;
            data.Losses += heroRef.Losses;
            data.DraftOrder += heroRef.DraftOrder;
            data.MaxAssists += heroRef.MaxAssists;
            data.MaxKills += heroRef.MaxKills;
            data.MinDeaths += heroRef.MinDeaths;
        }
    }
}
