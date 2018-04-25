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
    /*
    *   Skills:         410
    *   Ultimates:      124
    *   Singles:        534
    *   Singles/Type:   1068
    *   Pairs:          107878
    *   Pairs/Type:     215756
    */
    public static class FnMatchHandler
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("MatchHandler")]
        public static async Task Run(
            // Process only matches for AD Matches
            [BlobTrigger("hgv-matches/{id}.json")]CloudBlockBlob matchBlob, long id,
            // Output Queues - Singles, Pairs
            [Queue("hgv-stats-abilities")]ICollector<StatSnapshot> statsQueue,
            // Stats totals
            [Blob("hgv-stats/totals.json", System.IO.FileAccess.ReadWrite)]CloudBlockBlob totalsBlob,
            // List of valid Abilities
            [Blob("hgv-master/valid-abilities.json", System.IO.FileAccess.Read)]CloudBlockBlob abilitiesBlob,
            // List melee Heroes
            [Blob("hgv-master/heroes-melee.json", System.IO.FileAccess.Read)]CloudBlockBlob heroesBlob,
            // Logger
            TraceWriter log)
        {
            // Get Match
            var matchJson = await matchBlob.DownloadTextAsync();
            var match = JsonConvert.DeserializeObject<Match>(matchJson);

            var abilitiesJson = await abilitiesBlob.DownloadTextAsync();
            var validAbilities = JsonConvert.DeserializeObject<List<int>>(abilitiesJson);

            var heroesJson = await heroesBlob.DownloadTextAsync();
            var heroesMelee = JsonConvert.DeserializeObject<List<int>>(heroesJson);

            // Get Totals
            var jsonTotals = await totalsBlob.DownloadTextAsync();
            var totals = JsonConvert.DeserializeObject<Totals>(jsonTotals);
            var totalMatches = totals.Modes[(int)GameMode.ability_draft];

            // Get Skills, Pairs, & Quads - Add Snapshot to Queues
            ProcessMatch(log, statsQueue, validAbilities, heroesMelee, totalMatches, match);

            // Delete Match
            await matchBlob.DeleteAsync();
        }

        private static void ProcessMatch(TraceWriter log, 
            ICollector<StatSnapshot> statsQueue, 
            List<int> validAbilities, List<int> heroesMelee,
            int totalMatches,
            Match match)
        {
            try
            {
                foreach (var player in match.players)
                {
                    // Leaver Gruad
                    if (player.leaver_status != 0)
                        continue;

                    var upgrades = player.ability_upgrades
                           .Select(_ => _.ability)
                           .Distinct()
                           .Intersect(validAbilities)
                           .OrderBy(_ => _)
                           .ToList();

                    // Skill Count Warning
                    if (upgrades.Count < 4)
                    {
                        log.Warning($"Fn-HandleADMatch({match.match_id}): hero({player.hero_id}) has < 4 abilties.");
                    }
                    else if (upgrades.Count > 4)
                    {
                        log.Warning($"Fn-HandleADMatch({match.match_id}): hero({player.hero_id}) has > 4 abilties.");
                    }

                    var snapshot = new StatSnapshot
                    {
                        PartitionKey = heroesMelee.Contains(player.hero_id) ? "Melee" : "Range",
                        Win = player.player_slot < 6 ? match.radiant_win : !match.radiant_win,
                        Kills = player.kills,
                        Deaths = player.deaths,
                        Assists = player.assists,
                        Damage = player.hero_damage,
                        Destruction = player.tower_damage,
                        Gold = player.gold,
                        TotalMatches = totalMatches
                    };

                    CreatePairs(statsQueue, upgrades, snapshot);
                    CreateSingles(statsQueue, upgrades, snapshot);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Fn-HandleADMatch({match.match_id}): Error processing match", ex);
            }
        }

        private static void CreateSingles(ICollector<StatSnapshot> queue, List<int> upgrades, StatSnapshot snapshot)
        {
            foreach (var i in upgrades)
            {
                snapshot.RowKey = $"{i}";
                snapshot.Type = 1;

                queue.Add(snapshot);
            }
        }

        private static void CreatePairs(ICollector<StatSnapshot> queue, List<int> upgrades, StatSnapshot snapshot)
        {
            var pairs =
                from a in upgrades
                from b in upgrades
                where a.CompareTo(b) < 0
                orderby a, b
                select Tuple.Create(a, b);

            foreach (var p in pairs)
            {
                snapshot.RowKey = $"{p.Item1}-{p.Item2}";
                snapshot.Type = 2;

                queue.Add(snapshot);
            }
        }
    }
}
