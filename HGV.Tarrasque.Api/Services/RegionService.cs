using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Common.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Services
{
    public interface IRegionService
    {
        Task<List<RegionModel>> GetRegionsByDate(string date, CloudTable table, ILogger log);
        Task<List<RegionModel>> GetRegionsSummary(CloudTable table, ILogger log);
    }

    public class RegionService : IRegionService
    {
        public async Task<List<RegionModel>> GetRegionsByDate(string date, CloudTable table, ILogger log)
        {
            var query = new TableQuery<RegionEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, date)
            );
            var collection = new List<RegionEntity>();
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync<RegionEntity>(query, token);
                token = segment.ContinuationToken;
                collection.AddRange(segment.Results);
            }
            while (token != null);

            var models = collection
                .GroupBy(_ => _.RegionName)
                .Select(_ => new RegionModel() { Name = _.Key, Total = _.Sum(x => x.Total) })
                .OrderByDescending(_ => _.Total)
                .ToList();

            return models;
        }
        public async Task<List<RegionModel>> GetRegionsSummary(CloudTable table, ILogger log)
        {
            var filter = string.Empty;
            var dates = Enumerable.Range(0, 6).Select(_ => DateTime.UtcNow.AddDays(_ * -1).ToString("yy-MM-dd")).ToList();
            foreach (var date in dates)
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, date);
                }
                else
                {
                    filter = TableQuery.CombineFilters(filter,
                        TableOperators.Or,
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, date)
                    );
                }

            }

            var query = new TableQuery<RegionEntity>().Where(filter);
            var collection = new List<RegionEntity>();
            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync<RegionEntity>(query, token);
                token = segment.ContinuationToken;
                collection.AddRange(segment.Results);
            }
            while (token != null);

            var models = collection
                .GroupBy(_ => _.RegionName)
                .Select(_ => new RegionModel() { Name = _.Key, Total = _.Sum(x => x.Total) })
                .OrderByDescending(_ => _.Total)
                .ToList();

            return models;
        }
    }
}
