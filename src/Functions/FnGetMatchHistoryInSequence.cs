using HGV.Daedalus;
using HGV.Tarrasque.Models;
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

            // Config Gruad
            if (String.IsNullOrWhiteSpace(config.SteamKey))
            {
                log.Error($"Fn-GetMatchHistoryInSequence(): Error SteamKey not initialized.");
                return;
            }

            log.Info($"Fn-GetMatchHistoryInSequence({next.MatchNumber}): started at {DateTime.UtcNow}");

            // Seed [Next]
            await SeedNext(config, next, log);

            // Output [Matches]
            await ProcessMatches(config, next, serailizer, binder, log);

            // Output [Next]
            OutputNext(next, outputBlob, serailizer);
        }

        private static void OutputNext(Next next, TextWriter outputBlob, JsonSerializer serailizer)
        {
            serailizer.Serialize(outputBlob, next);
        }

        static private async Task SeedNext(Config config, Next next, TraceWriter log)
        {
            if (next.MatchNumber != 0)
                return;

            using (var client = new DotaApiClient(config.SteamKey))
            {
                var matches = await client.GetLastestMatches();
                next.MatchNumber = matches.Max(_ => _.match_seq_num) + 1;
                next.TotalMatches = 0;
            }
        }

        static private async Task ProcessMatches(Config config, Next next, JsonSerializer serailizer, Binder binder, TraceWriter log)
        {
            try
            {
                using (var client = new DotaApiClient(config.SteamKey))
                {
                    var matches = await client.GetMatchesInSequence(next.MatchNumber);
                    next.MatchNumber = matches.Max(_ => _.match_seq_num) + 1;

                    foreach (var match in matches)
                    {
                        // Duration Gruad
                        if (match.duration < 900)
                            return;

                        // Player Gruad
                        if (match.human_players != 10 || match.players.Count != 10)
                            return;

                        // Mode Gruad
                        if (config.ActiveModes.Contains(match.game_mode) == false)
                            continue;

                        next.TotalMatches++;

                        var attr = new BlobAttribute($"hgv-matches/{match.match_id}.json");
                        using (var writer = await binder.BindAsync<TextWriter>(attr))
                        {
                            serailizer.Serialize(writer, match);
                        }
                    }
                }
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
}