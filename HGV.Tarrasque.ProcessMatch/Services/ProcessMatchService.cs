﻿using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Entities;
using HGV.Tarrasque.Common.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq.Extensions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.Common.Algorithms;

namespace HGV.Tarrasque.ProcessMatch.Services
{
    public interface IProcessMatchService
    {
        Task ProcessMatch(Match match, IBinder binder, ILogger log);
    }

    public class ProcessMatchService : IProcessMatchService
    {
        public async Task ProcessMatch(Match match, IBinder binder, ILogger log)
        {
            var tasks = new List<Task>();

            tasks.Add(UpdateRegion(match, binder, log));
            tasks.Add(UpdateHeroes(match, binder, log));
            tasks.Add(UpdateAbilities(match, binder, log));
            tasks.Add(UpdateHeroCombos(match, binder, log));
            tasks.Add(UpdateAbilityCombos(match, binder, log));
            tasks.Add(UpdatePlayers(match, binder, log));
            await Task.WhenAll(tasks);
        }

        private static async Task UpdateRegion(Match match, IBinder binder, ILogger log)
        {
            try
            {
                var regionId = match.GetRegion();
                var regionName = MetaClient.Instance.Value.GetRegionName(regionId);

                var partitionKey = match.GetDate();
                var rowKey = regionId.ToString();

                var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVRegions"));
                var result = await table.ExecuteAsync(TableOperation.Retrieve<RegionEntity>(partitionKey, rowKey));
                var entity = result.Result as RegionEntity;
                if (entity == null)
                {
                    entity = new RegionEntity()
                    {
                        PartitionKey = partitionKey,
                        RowKey = rowKey,
                        RegionId = regionId,
                        RegionName = regionName
                    };
                }

                entity.Total++;

                await table.ExecuteAsync(TableOperation.InsertOrMerge(entity));
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static async Task UpdateHeroes(Match match, IBinder binder, ILogger log)
        {
            try
            {
                var partitionKey = match.GetDate();

                var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVHeroes"));
                var batch = new TableBatchOperation();

                foreach (var player in match.players)
                {
                    var hero = player.GetHero();
                    var rowKey = hero.Id.ToString();

                    var result = await table.ExecuteAsync(TableOperation.Retrieve<HeroEntity>(partitionKey, rowKey));
                    var entity = result.Result as HeroEntity;
                    if (entity == null)
                    {
                        entity = new HeroEntity()
                        {
                            PartitionKey = partitionKey,
                            RowKey = rowKey,
                            HeroId = hero.Id,
                            HeroName = hero.Name
                        };
                    }

                    // Update Total Count
                    entity.Total++;

                    // Update Win Count only if Victory
                    if (match.Victory(player))
                        entity.Wins++;
                    else
                        entity.Losses++;

                    // Compute Win Rate
                    entity.WinRate = (float)entity.Wins / (float)entity.Total;

                    batch.Add(TableOperation.InsertOrMerge(entity));
                }

                await table.ExecuteBatchAsync(batch);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static async Task UpdateAbilities(Match match, IBinder binder, ILogger log)
        {
            try
            {
                var partitionKey = match.GetDate();

                var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVAbilities"));
                var batch = new TableBatchOperation();

                foreach (var player in match.players)
                {
                    var skills = player.GetSkills();
                    foreach (var ability in skills)
                    {
                        var rowKey = ability.Id.ToString();

                        var result = await table.ExecuteAsync(TableOperation.Retrieve<AbilityEntity>(partitionKey, rowKey));
                        var entity = result.Result as AbilityEntity;
                        if (entity == null)
                        {
                            entity = new AbilityEntity()
                            {
                                PartitionKey = partitionKey,
                                RowKey = rowKey,
                                AbilityId = ability.Id,
                                AbilityName = ability.Name
                            };
                        }
                            
                        // Update Total Count
                        entity.Total++;

                        // Draft Priority
                        entity.Priority += 0;

                        // Update Ancestry only if ability matches the players hero
                        if (ability.HeroId == player.hero_id)
                            entity.Ancestry++;

                        // Update Win Count only if Victory
                        if (match.Victory(player))
                            entity.Wins++;
                        else
                            entity.Losses++;

                        // Compute Win Rate
                        entity.WinRate = (float)entity.Wins / (float)entity.Total;

                        batch.Add(TableOperation.InsertOrMerge(entity));
                    }
                }

                await table.ExecuteBatchAsync(batch);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static async Task UpdateHeroCombos(Match match, IBinder binder, ILogger log)
        {
            try
            {
                var partitionKey = match.GetDate();

                var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVHeroCombos"));
                var batch = new TableBatchOperation();

                foreach (var player in match.players)
                {
                    var hero = player.GetHero();
                    var skills = player.GetSkills();
                    foreach (var ability in skills)
                    {
                        var rowKey = $"{hero.Id}-{ability.Id}";

                        var result = await table.ExecuteAsync(TableOperation.Retrieve<HeroComboEntity>(partitionKey, rowKey));
                        var entity = result.Result as HeroComboEntity;
                        if (entity == null)
                        {
                            entity = new HeroComboEntity() 
                            {
                                PartitionKey = partitionKey, 
                                RowKey = rowKey,
                                AbilityId = ability.Id,
                                AbilityName = ability.Name,
                                HeroId = hero.Id,
                                HeroName = hero.Name
                            };
                        }

                        // Update Total Count
                        entity.Total++;

                        // Update Win Count only if Victory
                        if (match.Victory(player))
                            entity.Wins++;
                        else
                            entity.Losses++;

                        // Compute Win Rate
                        entity.WinRate = (float)entity.Wins / (float)entity.Total;

                        batch.Add(TableOperation.InsertOrMerge(entity));
                    }
                }

                await table.ExecuteBatchAsync(batch);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static async Task UpdateAbilityCombos(Match match, IBinder binder, ILogger log)
        {
            try
            {
                var partitionKey = match.GetDate();

                var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVAbilityCombos"));
                var collection = new List<TableOperation>();

                foreach (var player in match.players)
                {
                    var skills = player.GetPairs();
                    foreach (var pair in skills)
                    {
                        var reflection = new List<(Ability Primary, Ability Combo)>()
                        {
                            ( Primary : pair.Item1, Combo : pair.Item2 ),
                            ( Primary : pair.Item2, Combo : pair.Item1 )
                        };
                        foreach (var item in reflection)
                        {
                            var rowKey = $"{item.Primary.Id}-{item.Combo.Id}";
                            var result = await table.ExecuteAsync(TableOperation.Retrieve<AbilityComboEntity>(partitionKey, rowKey));
                            var entity = result.Result as AbilityComboEntity;
                            if (entity == null)
                            {
                                entity = new AbilityComboEntity() 
                                {
                                    PartitionKey = partitionKey,
                                    RowKey = rowKey,
                                    PrimaryAbilityId = item.Primary.Id,
                                    PrimaryAbilityName = item.Primary.Name,
                                    ComboAbilityId = item.Combo.Id,
                                    ComboAbilityName = item.Combo.Name,
                                };
                            }

                            // Update Total Count
                            entity.Total++;

                            // Update Win Count only if Victory
                            if (match.Victory(player))
                                entity.Wins++;
                            else
                                entity.Losses++;

                            // Compute Win Rate
                            entity.WinRate = (float)entity.Wins / (float)entity.Total;

                            collection.Add(TableOperation.InsertOrMerge(entity));
                        }
                    }
                }

                var batches = collection.Batch(99);
                foreach (var batch in batches)
                {
                    var operation = new TableBatchOperation();

                    foreach (var item in batch)
                        operation.Add(item);

                    await table.ExecuteBatchAsync(operation);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private const long CATCH_ALL_ACCOUNT = 4294967295;
        private static async Task UpdatePlayers(Match match, IBinder binder, ILogger log)
        {
            try
            {
                var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVPlayers"));
                var partitionKey = match.GetRegion().ToString();

                var collection = new List<PlayerRef>();
                foreach (var player in match.players)
                {
                    if (player.account_id == CATCH_ALL_ACCOUNT)
                        continue;

                    var rowKey = player.account_id.ToString();
                    var result = await table.ExecuteAsync(TableOperation.Retrieve<PlayerEntity>(partitionKey, rowKey));
                    var entity = result.Result as PlayerEntity;
                    if (entity == null)
                    {
                        entity = new PlayerEntity()
                        {
                            PartitionKey = partitionKey,
                            RowKey = rowKey,
                            AccountId = player.account_id,
                            Persona = player.persona,
                            SteamId = player.SteamId(),
                            Ranking = 1000,
                        };
                    }

                    // Update Total Count
                    entity.Total++;

                    // Update Win Count only if Victory
                    if (match.Victory(player))
                        entity.Wins++;
                    else
                        entity.Losses++;

                    // Compute Win Rate
                    entity.WinRate = (float)entity.Wins / (float)entity.Total;

                    collection.Add(new PlayerRef() { Player = player, Data = entity });
                }

                if (collection.Count == 0)
                    return;

                var rankings = new Dictionary<ulong, double>();
                foreach (var item in collection)
                {
                    var victory = match.Victory(item.Player);
                    var myTeam = item.Player.GetTeam();
                    var opponents = collection.Where(_ => _.Player.GetTeam() != myTeam);
                    if (opponents.Count() > 0)
                    {
                        var avgRanking = opponents.Average(_ => _.Data.Ranking);
                        var ranking = ELORankingSystem.Calucate(item.Data.Ranking, avgRanking, victory);
                        rankings.Add(item.Player.account_id, ranking);
                    }
                    else
                    {
                        rankings.Add(item.Player.account_id, item.Data.Ranking);
                    }
                }

                var batch = new TableBatchOperation();
                foreach (var item in collection)
                {
                    item.Data.Ranking = rankings[item.Player.account_id];

                    batch.Add(TableOperation.InsertOrMerge(item.Data));
                }

                await table.ExecuteBatchAsync(batch);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

    }
}
