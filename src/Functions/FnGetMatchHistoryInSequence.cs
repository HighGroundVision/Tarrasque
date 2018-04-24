using HGV.Daedalus;
using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
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
            [Blob("hgv-master/next.json", System.IO.FileAccess.ReadWrite)]TextWriter nextWriter,
            // Totals counts of the number of matches
            [Blob("hgv-stats/totals.json", System.IO.FileAccess.ReadWrite)]TextReader totalsReader,
            [Blob("hgv-stats/totals.json", System.IO.FileAccess.ReadWrite)]TextWriter totalsWriter,
            // Binder (dynamic output binding)
            Binder binder,
            // Logger
            TraceWriter log)
        {
            var serailizer = JsonSerializer.CreateDefault();
            var config = (Config)serailizer.Deserialize(configBlob, typeof(Config));
            var next = (Next)serailizer.Deserialize(tirggerBlob, typeof(Next));
            var totals = (Totals)serailizer.Deserialize(totalsReader, typeof(Totals));

            // Config Gruad
            if (String.IsNullOrWhiteSpace(config.SteamKey))
            {
                log.Error($"Fn-GetMatchHistoryInSequence(): Error SteamKey not initialized.");
                return;
            }

            //log.Info($"Fn-GetMatchHistoryInSequence({next.MatchNumber}): started at {DateTime.UtcNow}");

            // Seed [Next]
            await SeedNext(config, next, log);

            // Output [Matches]
            await ProcessMatches(serailizer, binder, log, config, next, totals);

            // Output [Next & Totals]
            OutputNext(serailizer, nextWriter, next, totalsWriter, totals);
        }

        private static void OutputNext(JsonSerializer serailizer, TextWriter nextWriter, Next next, TextWriter totalsWriter, Totals total)
        {
            serailizer.Serialize(totalsWriter, total);
            serailizer.Serialize(nextWriter, next);
        }

        static private async Task SeedNext(Config config, Next next, TraceWriter log)
        {
            if (next.MatchNumber != 0)
                return;

            using (var client = new DotaApiClient(config.SteamKey))
            {
                var matches = await client.GetLastestMatches();
                next.MatchNumber = matches.Max(_ => _.match_seq_num) + 1;
            }
        }

        static private async Task ProcessMatches(JsonSerializer serailizer, Binder binder, TraceWriter log, Config config, Next next, Totals totals)
        {
            try
            {
                using (var client = new DotaApiClient(config.SteamKey))
                {
                    var matches = await client.GetMatchesInSequence(next.MatchNumber);
                    next.MatchNumber = matches.Max(_ => _.match_seq_num) + 1;

                    foreach (var match in matches)
                    {
                        // Count the game modes
                        totals.Modes[match.game_mode]++;

                        // Duration Gruad
                        if (match.duration < 900)
                            return;

                        // Player Gruad
                        if (match.human_players != 10 || match.players.Count != 10)
                            return;

                        // Mode Gruad
                        if (config.ActiveModes.Contains(match.game_mode) == false)
                            continue;

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