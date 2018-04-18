using System;
using System.Linq;
using System.IO;
using HGV.Daedalus.GetMatchDetails;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace HGV.Tarrasque.Functions
{
    public static class FnHandleADMatch
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("HandleAbilityDraftMatch")]
        public static void Run(
            [BlobTrigger("hgv-matches/{day}/18/{id}")]TextReader inputBlob, int day, long id, 
            TraceWriter log)
        {
            log.Info($"Fn-HandleAbilityDraftMatch({id}) executed at: {DateTime.UtcNow}");

            var serailizer = JsonSerializer.CreateDefault();
            var match = (Match)serailizer.Deserialize(inputBlob, typeof(Match));

            // Player Gruad
            if (match.human_players != 10 || match.players.Count != 10)
                return;

            foreach (var player in match.players)
            {
                var abilities = player.ability_upgrades.Select(_ => _.ability).Distinct().OrderBy(_ => _).ToList();
                
                // Abilities (X1)
                // Combos (X2)[16]
                // Drafts (x4)
            }
        }
    }
}
