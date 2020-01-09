using Dawn;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessHeroAbilities.Services
{
    public interface IProcessHeroAbilitiesService
    {
        Task ProcessHeroAbility(HeroAbilityReference haRef, TextReader reader, TextWriter writer);
    }

    public class ProcessHeroAbilitiesService : IProcessHeroAbilitiesService
    {
        public ProcessHeroAbilitiesService()
        {
        }

        public async Task ProcessHeroAbility(HeroAbilityReference haRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(haRef, nameof(haRef)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            if (reader == null)
                await NewPair(haRef, writer);
            else
                await UpdatePair(haRef, reader, writer);
        }

        private static async Task NewPair(HeroAbilityReference haRef, TextWriter writer)
        {
            var data = new HeroAbilityData();
            data.Region = haRef.Region;
            data.Date = haRef.Date;
            data.HeroId = haRef.Hero;
            data.AbilityId = haRef.Ability;

            SetData(haRef, data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static async Task UpdatePair(HeroAbilityReference haRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(haRef, nameof(haRef)).NotNull();
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var input = await reader.ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<HeroAbilityData>(input);

            SetData(haRef, data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static void SetData(HeroAbilityReference haRef, HeroData data)
        {
            data.Total++;
            data.Wins += haRef.Wins;
            data.Losses += haRef.Losses;
            data.DraftOrder += haRef.DraftOrder;
            data.MaxAssists += haRef.MaxAssists;
            data.MaxKills += haRef.MaxKills;
            data.MinDeaths += haRef.MinDeaths;
        }
    }
}
