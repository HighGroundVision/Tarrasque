using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Collection.Services
{
    public interface IProcessService
    {
        Task Process(long id, TextWriter writerwMatch);
    }

    public class ProcessService : IProcessService
    {
        private readonly IDotaApiClient client;

        public ProcessService(IDotaApiClient client)
        {
            this.client = client;
        }

        public async Task Process(long id, TextWriter writerwMatch)
        {
            // TODO: Error Trap
            var match = await client.GetMatchDetails(id);

            // TOOD: Store Match in Blob
            await StoreMatch(match, writerwMatch);

            // TODO: Update Region Summary [Day, Region]
            // - Total: #

            // TOOD: Loop Through Players

            // TOOD: Update Player Summary [Day, Region, AccountId]
            // - Matches: []
            // - Total: #
            // - Wins: #
            // - Loses: #

            // TOOD: Update Hero Summary [Day, Region, HeroId]
            // - Total: #
            // - Wins: #
            // - Loses: #

            // TOOD: Update Ability Summary [Day, Region, AbilityId]
            // - Total: #
            // - Wins: #
            // - Loses: #
            // - DraftOrder: #
            // - MaxKills: #
            // - MaxAssists: #
            // - MinDeaths: #
            // - MaxGold: #
            // - MaxGPM: #
        }

        private static async Task StoreMatch(Match match, TextWriter writerwMatch)
        {
            var json = JsonConvert.SerializeObject(match);
            await writerwMatch.WriteAsync(json);
        }
    }
}
