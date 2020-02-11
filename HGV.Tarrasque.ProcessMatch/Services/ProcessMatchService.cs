using Dawn;
using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessMatch.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var start = DateTime.Now;
            var tasks = new List<Task>();

            tasks.Add(UpdateRegion(match, binder, log));
            tasks.Add(UpdateHeroes(match, binder, log));
            tasks.Add(UpdateAbilities(match, binder, log));
            tasks.Add(UpdateTalents(match, binder, log));
            tasks.Add(UpdateHeroCombos(match, binder, log));
            tasks.Add(UpdateAbilityCombos(match, binder, log));
            await Task.WhenAll(tasks);

            var delta = DateTime.Now - start;
            log.LogWarning($"ProcessMatch: {delta}");
        }

        private static async Task UpdateRegion(Match match, IBinder binder, ILogger log)
        {
            var timestamp = match.GetDate();
            var region = match.GetRegion().ToString();

            var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVRegions"));
            var result = await table.ExecuteAsync(TableOperation.Retrieve<RegionEntity>(timestamp, region));
            var entity = result.Result as RegionEntity;
            if(entity == null)
                entity = new RegionEntity() { PartitionKey = timestamp, RowKey = region };

            entity.Total++;

            await table.ExecuteAsync(TableOperation.InsertOrMerge(entity));
        }

        private static async Task UpdateHeroes(Match match, IBinder binder, ILogger log)
        {
            var partitionKey = match.GetDate();

            var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVHeroes"));

            foreach (var player in match.players)
            {
                var rowKey = player.hero_id.ToString();

                var result = await table.ExecuteAsync(TableOperation.Retrieve<HeroEntity>(partitionKey, rowKey));
                var entity = result.Result as HeroEntity;
                if (entity == null)
                    entity = new HeroEntity() { PartitionKey = partitionKey, RowKey = rowKey };

                // Update Total Count
                entity.Total++;

                // Update Win Count only if Victory
                if (match.Victory(player))
                    entity.Wins++;
                else
                    entity.Losses++;

                // Compute Win Rate
                entity.WinRate = (float)entity.Wins / (float)entity.Total;

                await table.ExecuteAsync(TableOperation.InsertOrMerge(entity));
            }
        }

        private static async Task UpdateAbilities(Match match, IBinder binder, ILogger log)
        {
            var partitionKey = match.GetDate();

            var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVAbilities"));

            foreach (var player in match.players)
            {
                var skills = player.GetSkills();
                foreach (var ability in skills)
                {
                    var rowKey = ability.Id.ToString();

                    var result = await table.ExecuteAsync(TableOperation.Retrieve<AbilityEntity>(partitionKey, rowKey));
                    var entity = result.Result as AbilityEntity;
                    if (entity == null)
                        entity = new AbilityEntity() { PartitionKey = partitionKey, RowKey = rowKey };

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

                    await table.ExecuteAsync(TableOperation.InsertOrMerge(entity));
                }
            }
        }

        private static async Task UpdateTalents(Match match, IBinder binder, ILogger log)
        {
            var partitionKey = match.GetDate();

            var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVTalents"));

            foreach (var player in match.players)
            {
                var talents = player.GetTalenets();
                foreach (var talent in talents)
                {
                    var rowKey = talent.Id.ToString();

                    var result = await table.ExecuteAsync(TableOperation.Retrieve<TalentEntity>(partitionKey, rowKey));
                    var entity = result.Result as TalentEntity;
                    if (entity == null)
                        entity = new TalentEntity() { PartitionKey = partitionKey, RowKey = rowKey };

                    // Update Total Count
                    entity.Total++;

                    // Update Win Count only if Victory
                    if (match.Victory(player))
                        entity.Wins++;
                    else
                        entity.Losses++;

                    // Compute Win Rate
                    entity.WinRate = (float)entity.Wins / (float)entity.Total;

                    await table.ExecuteAsync(TableOperation.InsertOrMerge(entity));
                }
            }
        }

        private static async Task UpdateHeroCombos(Match match, IBinder binder, ILogger log)
        {
            var partitionKey = match.GetDate();

            var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVHeroCombos"));

            foreach (var player in match.players)
            {
                var skills = player.GetSkills();
                foreach (var ability in skills)
                {
                    var rowKey = $"{player.hero_id}-{ability.Id}";

                    var result = await table.ExecuteAsync(TableOperation.Retrieve<HeroComboEntity>(partitionKey, rowKey));
                    var entity = result.Result as HeroComboEntity;
                    if (entity == null)
                        entity = new HeroComboEntity() { PartitionKey = partitionKey, RowKey = rowKey };

                    // Update Total Count
                    entity.Total++;

                    // Update Win Count only if Victory
                    if (match.Victory(player))
                        entity.Wins++;
                    else
                        entity.Losses++;

                    // Compute Win Rate
                    entity.WinRate = (float)entity.Wins / (float)entity.Total;

                    await table.ExecuteAsync(TableOperation.InsertOrMerge(entity));
                }
            }
        }

        private static async Task UpdateAbilityCombos(Match match, IBinder binder, ILogger log)
        {
            var partitionKey = match.GetDate();

            var table = await binder.BindAsync<CloudTable>(new TableAttribute("HGVAbilityCombos"));

            foreach (var player in match.players)
            {
                var skills = player.GetPairs();
                foreach (var pair in skills)
                {
                    var collection = new List<string>()
                    {
                        $"{pair.Item1.Id}-{pair.Item2.Id}",
                        $"{pair.Item2.Id}-{pair.Item1.Id}"
                    };
                    foreach (var rowKey in collection)
                    {
                        var result = await table.ExecuteAsync(TableOperation.Retrieve<AbilityComboEntity>(partitionKey, rowKey));
                        var entity = result.Result as AbilityComboEntity;
                        if (entity == null)
                            entity = new AbilityComboEntity() { PartitionKey = partitionKey, RowKey = rowKey };

                        // Update Total Count
                        entity.Total++;

                        // Update Win Count only if Victory
                        if (match.Victory(player))
                            entity.Wins++;
                        else
                            entity.Losses++;

                        // Compute Win Rate
                        entity.WinRate = (float)entity.Wins / (float)entity.Total;

                        await table.ExecuteAsync(TableOperation.InsertOrMerge(entity));
                    }
                }
            }
        }
    }
}
