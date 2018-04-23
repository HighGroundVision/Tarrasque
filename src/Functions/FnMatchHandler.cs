using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnMatchHandler
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("MatchHandler")]
        public static async Task Run(
            // Process only matches for AD Matches
            [BlobTrigger("hgv-matches/{id}.json")]CloudBlockBlob matchBlob, long id,
            // Output Queues - Drafts, Combos, Abilities
            [Queue("hgv-drafts")]ICollector<StatSnapshot> draftsQueue,
            [Queue("hgv-combos")]ICollector<StatSnapshot> combosQueue,
            [Queue("hgv-abilities")]ICollector<StatSnapshot> abilitiesQueue,
            // Logger
            TraceWriter log)
        {
            log.Info($"Fn-HandleADMatch({id}): started at {DateTime.UtcNow}");

            // Get Match
            var matchJson = await matchBlob.DownloadTextAsync();
            var match = JsonConvert.DeserializeObject<Match>(matchJson);

            // Get Skills, Pairs, & Quads - Add Snapshot to Queues
            ProcessMatch(log, draftsQueue, combosQueue, abilitiesQueue, match);

            // Delete Match
            await matchBlob.DeleteAsync();
        }

        private static void ProcessMatch(TraceWriter log, ICollector<StatSnapshot> draftsQueue, ICollector<StatSnapshot> combosQueue, ICollector<StatSnapshot> abilitiesQueue, Match match)
        {
            try
            {
                foreach (var player in match.players)
                {
                    var upgrades = player.ability_upgrades
                           .Select(_ => _.ability)
                           .Distinct()
                           .OrderBy(_ => _)
                           .ToList();

                    // Ability Gruad
                    if (upgrades.Count < 4)
                        continue;

                    //var result 
                    var snapshot = new StatSnapshot
                    {
                        Win = player.player_slot < 6 ? match.radiant_win : !match.radiant_win,
                        Kills = player.kills,
                        Deaths = player.death,
                        Assists = player.assists,
                        Damage = player.hero_damage,
                        Destruction = player.tower_damage,
                        Gold = player.gold
                    };

                    // Drafts
                    CreateQuads(draftsQueue, upgrades, snapshot);

                    // Combos
                    CreatePairs(combosQueue, upgrades, snapshot);

                    // Abilities
                    CreateSkills(abilitiesQueue, upgrades, snapshot);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Fn-HandleADMatch({match.match_id}): Error processing match", ex);
            }
        }

        private static void CreateSkills(ICollector<StatSnapshot> abilitiesQueue, List<int> upgrades, StatSnapshot snapshot)
        {
            foreach (var i in upgrades)
            {
                snapshot.Key = $"{i}";

                abilitiesQueue.Add(snapshot);
            }
        }

        private static void CreatePairs(ICollector<StatSnapshot> combosQueue, List<int> upgrades, StatSnapshot snapshot)
        {
            var pairs =
                from a in upgrades
                from b in upgrades
                where a.CompareTo(b) < 0
                orderby a, b
                select Tuple.Create(a, b);

            foreach (var p in pairs)
            {
                snapshot.Key = $"{p.Item1}-{p.Item2}";

                combosQueue.Add(snapshot);
            }
        }

        private static void CreateQuads(ICollector<StatSnapshot> draftsQueue, List<int> upgrades, StatSnapshot snapshot)
        {
            var quads =
                from a in upgrades
                from b in upgrades
                from c in upgrades
                from d in upgrades
                where a.CompareTo(b) < 0 && b.CompareTo(c) < 0 & c.CompareTo(d) < 0
                orderby a, b, c, d
                select Tuple.Create(a, b, c, d);

            foreach (var q in quads)
            {
                snapshot.Key = $"{q.Item1}-{q.Item2}-{q.Item3}-{q.Item4}";

                draftsQueue.Add(snapshot);
            }
        }
    }
}
