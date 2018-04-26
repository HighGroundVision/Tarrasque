using System;
using System.Threading.Tasks;
using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace HGV.Tarrasque.Functions
{
    public static class FnStatsAbilitiesHandler
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("StatsAbilitiesHandler")]
        public static async Task Run(
            // Queue w/ Snapshot
            [QueueTrigger("hgv-stats-abilities")]StatSnapshot snapshot,
            // Abilities table 
            [Table("HGVStatsAbilities")]CloudTable statsTables,
            // Logger
            TraceWriter log
        )
        {
            // Gruad: Types
            if (snapshot.Type == 0)
            {
                log.Error("Fn-StatHandler(): invalid Snapshot type");
                return;
            }

            // Get Item
            var operation = TableOperation.Retrieve<AbilityStats>(snapshot.PartitionKey, snapshot.RowKey);
            var result = await statsTables.ExecuteAsync(operation);
            var entity = result.Result as AbilityStats;

            // If Item dose not exist Create it
            if (entity == null)
                entity = new AbilityStats() { PartitionKey = snapshot.PartitionKey, RowKey = snapshot.RowKey, Type = snapshot.Type };

            // Update stats
            entity.Picks++;
            entity.Wins += snapshot.Win ? 1 : 0;
            entity.Kills += snapshot.Kills;
            entity.Deaths += snapshot.Deaths;
            entity.Assists += snapshot.Assists;
            entity.Destruction += snapshot.Destruction;
            entity.Damage += snapshot.Damage;
            entity.Gold += snapshot.Gold;

            // Insert or Replace table item
            operation = TableOperation.InsertOrReplace(entity);
            await statsTables.ExecuteAsync(operation);
        }
    }
}
