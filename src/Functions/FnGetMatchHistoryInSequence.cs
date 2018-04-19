using HGV.Daedalus;
using HGV.Tarrasque.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnGetMatchHistoryInSequence
    {
        /*
        Heroes	        104 
        Skills	        312 
        Utlimates	    98 
        Abilities	    410 
        Skill Triads    97,032 
        Ability Drafts  9,509,136 
        */

        [FunctionName("GetMatchHistoryInSequence")]
        [StorageAccount("AzureWebJobsStorage")]
        public static async Task Run(
            // Next File holding Json with the next match # as Input
            [BlobTrigger("hgv-master/next.json")]TextReader tirggerBlob,
            // Configuration File
            [Blob("hgv-master/config.json", System.IO.FileAccess.Read)]TextReader configBlob,
            // Next File holding Json with the next match # as Output
            [Blob("hgv-master/next.json", System.IO.FileAccess.ReadWrite)]TextWriter outputBlob,
            // Binder (dynamic output binding)
            Binder binder,
            // Logger
            TraceWriter log)
        {
            var serailizer = JsonSerializer.CreateDefault();
            var config = (Config)serailizer.Deserialize(configBlob, typeof(Config));
            var next = (Next)serailizer.Deserialize(tirggerBlob, typeof(Next));

            if (String.IsNullOrWhiteSpace(config.SteamKey))
            {
                log.Error($"Fn-GetMatchHistoryInSequence(): Error SteamKey not initialized.");
                return;
            }

            if (next.MatchNumber == 0)
            {
                log.Error($"Fn-GetMatchHistoryInSequence(): Error MatchNumber not initialized.");
                return;
            }

            log.Info($"Fn-GetMatchHistoryInSequence({next.MatchNumber}): started at {DateTime.UtcNow}");

            using (var client = new DotaApiClient(config.SteamKey))
            {
                var matches = await client.GetMatchesInSequence(next.MatchNumber);
                next.MatchNumber = matches.Max(_ => _.match_seq_num) + 1;

                foreach (var match in matches)
                {
                    var day = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(match.start_time).DayOfYear;
                    var attr = new BlobAttribute($"hgv-matches/{day}/{match.game_mode:00}/{match.match_id}");
                    using (var writer = await binder.BindAsync<TextWriter>(attr))
                    {
                        serailizer.Serialize(writer, match);
                    }
                }
            }

            serailizer.Serialize(outputBlob, next);
        }
    }
}