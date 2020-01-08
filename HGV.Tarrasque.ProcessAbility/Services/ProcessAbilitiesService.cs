using Dawn;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessAbility.Services
{
    public interface IProcessAbilitiesService
    {
        Task ProcessAbility(AbilityReference haRef, TextReader reader, TextWriter writer);
    }

    public class ProcessAbilitiesService : IProcessAbilitiesService
    {
        public ProcessAbilitiesService()
        {
        }

        public async Task ProcessAbility(AbilityReference abilityRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(abilityRef, nameof(abilityRef)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            if (reader == null)
                await NewAbility(abilityRef, writer);
            else
                await UpdateAbility(abilityRef, reader, writer);
        }

        private static async Task NewAbility(AbilityReference abilityRef, TextWriter writer)
        {
            var data = new AbilityData();
            data.Region = abilityRef.Region;
            data.Date = abilityRef.Date;
            data.AbilityId = abilityRef.Ability;

            SetData(abilityRef, data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static async Task UpdateAbility(AbilityReference abilityRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(abilityRef, nameof(abilityRef)).NotNull();
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var input = await reader.ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<AbilityData>(input);

            SetData(abilityRef, data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static void SetData(AbilityReference abilityRef, AbilityData data)
        {
            data.Total++;
            data.Wins += abilityRef.Wins;
            data.Losses += abilityRef.Losses;
            data.DraftOrder += abilityRef.DraftOrder;
            data.MaxAssists += abilityRef.MaxAssists;
            data.MaxKills += abilityRef.MaxKills;
            data.MinDeaths += abilityRef.MinDeaths;
            data.HeroAbility += abilityRef.HeroAbility;
        }
    }
}
