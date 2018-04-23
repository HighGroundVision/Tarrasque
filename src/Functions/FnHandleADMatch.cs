using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnHandleADMatch
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("HandleADMatch")]
        public static async Task Run(
            // Process only matches for AD (18) matches
            [BlobTrigger("hgv-matches/{id}")]TextReader inputBlob, long id,
            // Configuration File
            [Blob("hgv-master/valid-abilities.json", System.IO.FileAccess.Read)]TextReader abilitiesBlob,
            // Logger
            TraceWriter log)
        {
            log.Info($"Fn-HandleADMatch({id}): started at {DateTime.UtcNow}");

            var serailizer = JsonSerializer.CreateDefault();
            var match = (Match)serailizer.Deserialize(inputBlob, typeof(Match));
            var validAbilities = (List<int>)serailizer.Deserialize(abilitiesBlob, typeof(List<int>));

            foreach (var player in match.players)
            {
                try
                {
                    var upgrades = player.ability_upgrades.Select(_ => _.ability).Distinct().ToList();
                    var skills = upgrades.Intersect(validAbilities).OrderBy(_ => _).ToList();
                    
                    // Ability Gruad
                    if (skills.Count != 4)
                        continue;

                    var result = player.player_slot < 6 ? match.radiant_win : !match.radiant_win;

                    // Drafts(4)
                    //await ProcessDraft(day, drafts, player, skills, result);

                    // Combos(2)[x6]
                    //await ProcessCombos(day, combos, player, skills, result);

                    // Abilities(1)[X4]
                    //await ProcessAbilties(day, abilities, player, skills, result);
                }
                catch (Exception ex)
                {
                    log.Error($"Fn-HandleAbilityDraftMatch({id}): Error processing Player({player.player_slot}) abilities", ex);
                }
            }
        }

        private static async Task ProcessDraft(int day, CloudTable table, Player player, List<int> upgrades, bool result)
        {
            var entity = new DraftCount(day, upgrades[0], upgrades[1], upgrades[2], upgrades[3]);
            await CreateOrUpdate(table, player, result, entity);
        }

        private static async Task ProcessCombos(int day, CloudTable table, Player player, List<int> upgrades, bool result)
        {
            var pairs = upgrades.SelectMany((lhs, i) => upgrades.Skip(i + 1).Select(rhs => Tuple.Create<int, int>(lhs, rhs)));
            foreach (var pair in pairs)
            {
                var entity = new ComboCount(day, pair.Item1, pair.Item2);
                await CreateOrUpdate(table, player, result, entity);
            }
        }

        private static async Task ProcessAbilties(int day, CloudTable table, Player player, List<int> upgrades, bool result)
        {
            foreach (var ability in upgrades)
            {
                var entity = new AbilityCount(day, ability);
                await CreateOrUpdate(table, player, result, entity);
            }
        }

        private static async Task CreateOrUpdate<T>(CloudTable table, Player player, bool result, T entity) where T : AbiltiyDraftCounts
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey);
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
            if (retrievedResult.Result == null)
            {
                // Create
                entity.Kills = player.kills;
                entity.Wins = result ? 1 : 0;
                entity.Deaths = player.death;
                entity.Assist = player.assists;

                TableOperation insertOperation = TableOperation.Insert(entity);
                await table.ExecuteAsync(insertOperation);
            }
            else
            {
                // Update
                entity = (T)retrievedResult.Result;
                entity.Wins += result ? 1 : 0;
                entity.Kills += player.kills;
                entity.Deaths += player.death;
                entity.Assist += player.assists;
                entity.Picks++;

                // Replace
                TableOperation updateOperation = TableOperation.Replace(entity);
                await table.ExecuteAsync(updateOperation);

            }
        }
    }
}
