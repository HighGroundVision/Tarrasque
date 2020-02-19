
using HGV.Basilius;
using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Common.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Functions
{
    public class FnRegionAPI
    {

        [FunctionName("FnDailyRegionCount")]
        public IActionResult GetDailyRegionCount(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "daily/region/{date}/{region}")] HttpRequest req,
            [Table("HGVRegions", "{date}", "{region}")]RegionEntity entity,
            ILogger log)
        {
            if (entity == null)
                return new NotFoundResult();

            var model = new RegionModel()
            {
                Name = entity.RegionName,
                Total = entity.Total,
            };

            return new OkObjectResult(model);
        }

        [FunctionName("FnDailyRegionsCount")]
        public async Task<IActionResult> GetDailyRegionsCount(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "daily/regions/{date}")] HttpRequest req,
            string date,
            [Table("HGVRegions")]CloudTable table,
            ILogger log)
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

            return new OkObjectResult(models);
        }

        [FunctionName("FnSummaryRegionsCount")]
        public async Task<IActionResult> GetSummaryRegionsCount(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "summary/regions")] HttpRequest req,
            [Table("HGVRegions")]CloudTable table,
            ILogger log)
        {
            var filter = string.Empty;
            var dates = Enumerable.Range(0, 6).Select(_ => DateTime.UtcNow.AddDays(_ * -1).ToString("yy-MM-dd")).ToList();
            foreach (var date in dates)
            {
                if(string.IsNullOrWhiteSpace(filter))
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

            return new OkObjectResult(models);
        }
    }
}
