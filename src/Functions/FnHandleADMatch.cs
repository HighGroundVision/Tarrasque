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
            // Process only matches for that {day} and only for AD (18) matches
            [BlobTrigger("hgv-matches/{day}/18/{id}")]TextReader inputBlob, int day, long id,
            // Table to store the ability counts
            [Table("hgv-ad-stats-abilities")]CloudTable abilities,
            // Table to store the combo counts
            [Table("hgv-ad-stats-combos")]CloudTable combos,
            // Table to store the draft counts
            [Table("hgv-ad-stats-drafts")]CloudTable drafts,
            // Logger
            TraceWriter log)
        {
            log.Info($"Fn-HandleADMatch({id}): started at {DateTime.UtcNow}");

            var serailizer = JsonSerializer.CreateDefault();
            var match = (Match)serailizer.Deserialize(inputBlob, typeof(Match));

            // Duration Gruad
            if (match.duration < 900)
                return;

            // Player Gruad
            if (match.human_players != 10 || match.players.Count != 10)
                return;

            foreach (var player in match.players)
            {
                try
                {
                    var upgrades = player.ability_upgrades.Select(_ => _.ability).Distinct().OrderBy(_ => _).ToList();

                    // Ability Gruad
                    if (upgrades.Count != 4)
                        continue;

                    var result = player.player_slot < 6 ? match.radiant_win : !match.radiant_win;

                    // Drafts(4)
                    await ProcessDraft(day, drafts, player, upgrades, result);

                    // Combos(2)[x6]
                    await ProcessCombos(day, combos, player, upgrades, result);

                    // Abilities(1)[X4]
                    await ProcessAbilties(day, abilities, player, upgrades, result);
                }
                catch (Exception ex)
                {
                    log.Error($"Fn-HandleAbilityDraftMatch({id}): Error processing Player({player.player_slot}) abilities", ex);
                }
            }
        }

        private static async Task ProcessDraft(int day, CloudTable table, Player player, List<int> upgrades, bool result)
        {
            var entity = new DraftADStat(day, upgrades[0], upgrades[1], upgrades[2], upgrades[4]);
            await CreateOrUpdate(table, player, result, entity);
        }

        private static async Task ProcessCombos(int day, CloudTable table, Player player, List<int> upgrades, bool result)
        {
            var pairs = upgrades.SelectMany((lhs, i) => upgrades.Skip(i + 1).Select(rhs => Tuple.Create<int, int>(lhs, rhs)));
            foreach (var pair in pairs)
            {
                var entity = new ComboADStat(day, pair.Item1, pair.Item2);
                await CreateOrUpdate(table, player, result, entity);
            }
        }

        private static async Task ProcessAbilties(int day, CloudTable table, Player player, List<int> upgrades, bool result)
        {
            foreach (var ability in upgrades)
            {
                var entity = new AbilityADStat(day, ability);
                await CreateOrUpdate(table, player, result, entity);
            }
        }

        private static async Task CreateOrUpdate<T>(CloudTable table, Player player, bool result, T entity) where T : AbiltiyDraftStat
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey);
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
            if (retrievedResult.Result == null)
            {
                // Create
                entity.Kills = player.kills;
                entity.Wins = result ? 1 : 0;

                TableOperation insertOperation = TableOperation.Insert(entity);
                await table.ExecuteAsync(insertOperation);
            }
            else
            {
                // Update
                entity = (T)retrievedResult.Result;
                entity.Kills += player.kills;
                entity.Wins += result ? 1 : 0;
                entity.Picks++;

                // Replace
                TableOperation updateOperation = TableOperation.Replace(entity);
                await table.ExecuteAsync(updateOperation);

            }
        }
    }
}
