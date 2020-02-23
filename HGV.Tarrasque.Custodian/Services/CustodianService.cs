using HGV.Tarrasque.Common.Entities;
using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MoreLinq;

namespace HGV.Tarrasque.Custodian.Services
{
    public interface ICustodianService
    {
        Task Process(CustodianModel model, IBinder binding, ILogger log);
    }

    public class CustodianService : ICustodianService
    {
        public CustodianService()
        {
        }

        public async Task Process(CustodianModel model, IBinder binding, ILogger log)
        {
            using (new Timer($"RegionEntity", log))
            {
                await DeleteEntities("HGVRegions", model, binding);
            }
            using (new Timer($"HeroEntity", log))
            {
                await DeleteEntities("HGVHeroes", model, binding);
            }
            using (new Timer($"AbilityEntity", log))
            {
                await DeleteEntities("HGVAbilities", model, binding);
            }
            using (new Timer($"HeroComboEntity", log))
            {
                await DeleteEntities("HGVHeroCombos", model, binding);
            }
            using (new Timer($"AbilityComboEntity", log))
            {
                await DeleteEntities("HGVAbilityCombos", model, binding);
            }
        }

        private async Task DeleteEntities(string tableName, CustodianModel model, IBinder binding)
        {
            var table = await binding.BindAsync<CloudTable>(new TableAttribute(tableName));
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, model.Date);
            var query = new TableQuery<TableEntity>().Where(filter).Take(100).Select(new List<string>() { "PartitionKey", "RowKey" });
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, token);
                token = segment.ContinuationToken;

                var batch = new TableBatchOperation();
                foreach (var item in segment.Results)
                    batch.Add(TableOperation.Delete(item));

                if(batch.Count > 0)
                    await table.ExecuteBatchAsync(batch);
            }
            while (token != null);
        }
    }
}
