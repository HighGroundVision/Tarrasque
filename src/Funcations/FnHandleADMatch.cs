using HGV.Daedalus.GetMatchDetails;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public class MatchResult
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Text { get; set; }
    }

    public static class FnHandleADMatch
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("HandleAbilityDraftMatch")]
        public static void Run(
            [BlobTrigger("hgv-matches/{day}/18/{id}")]TextReader inputBlob, int day, long id,
            [Table("hgv-ad-abilities")]ICollector<MatchResult> tableBinding1,
            [Table("hgv-ad-combos")]ICollector<MatchResult> tableBinding2,
            [Table("hgv-ad-drafts")]ICollector<MatchResult> tableBinding3,
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

                /*
                var attr = new BlobAttribute($"hgv-matches/{day}/{match.game_mode:00}/{match.match_id}");
                using (var writer = await binder.BindAsync<TextWriter>(attr))
                {}   
                */
            }
        }
    }
}
