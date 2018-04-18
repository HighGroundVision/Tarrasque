using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using HGV.Daedalus;
using Newtonsoft.Json;

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
            [BlobTrigger("hgv-master/next.json")]TextReader tirggerBlob,
            [Blob("hgv-master/config.json", System.IO.FileAccess.Read)]TextReader configBlob,
            [Blob("hgv-master/next.json", System.IO.FileAccess.ReadWrite)]TextWriter outputBlob,
            Binder binder,
            TraceWriter log)
        {
            var serailizer = JsonSerializer.CreateDefault();
            var config = (Config)serailizer.Deserialize(configBlob, typeof(Config));
            var next = (Next)serailizer.Deserialize(tirggerBlob, typeof(Next));

            if (String.IsNullOrWhiteSpace(config.SteamKey))
                throw new ApplicationException("config.json has not been initalized.");

            if (next.MatchNumber == 0)
                throw new ApplicationException("next.json has not been initalized.");

            log.Info($"Fn-GetMatchHistoryInSequence({next.MatchNumber}) executed at: {DateTime.UtcNow}");

            using (var client = new DotaApiClient(config.SteamKey))
            {
                var matches = await client.GetMatchesInSequence(next.MatchNumber);
                next.MatchNumber = matches.Max(_ => _.match_seq_num) + 1;

                foreach (var match in matches)
                {
                    // Duration Gruad
                    if (match.duration < 900)
                        continue;

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