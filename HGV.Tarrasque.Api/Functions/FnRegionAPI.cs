
using HGV.Basilius;
using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Api.Services;
using HGV.Tarrasque.Common.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Functions
{
    public class FnRegionAPI
    {
        private readonly IReadOnlyPolicyRegistry<string> policyRegistry;
        private readonly IRegionService regionService;

        public FnRegionAPI(IReadOnlyPolicyRegistry<string> policyRegistry, IRegionService regionService)
        {
            this.policyRegistry = policyRegistry;
            this.regionService = regionService;
        }

        [FunctionName("FnSummaryRegionsCount")]
        public async Task<IActionResult> GetSummaryRegionsCount(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "summary/regions")] HttpRequest req,
            [Table("HGVRegions")]CloudTable table,
            ILogger log)
        {
            var cachePolicy = policyRegistry.Get<IAsyncPolicy<List<RegionModel>>>("FnSummaryRegionsCount");
            var collection = await cachePolicy.ExecuteAsync(
               context => regionService.GetRegionsSummary(table, log),
               new Context("FnSummaryRegionsCount")
            );

            return new OkObjectResult(collection);
        }

        [FunctionName("FnDailyRegionsCount")]
        public async Task<IActionResult> GetDailyRegionsCount(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "daily/regions/{date}")] HttpRequest req,
            [Table("HGVRegions")]CloudTable table,
            string date,
            ILogger log)
        {
            var context = new Context("FnDailyRegionsCount");
            context["date"] = date;
            var cachePolicy = policyRegistry.Get<IAsyncPolicy<List<RegionModel>>>("FnDailyRegionsCount");
            var collection = await cachePolicy.ExecuteAsync(
               context => regionService.GetRegionsByDate(date, table, log),
               context
            );

            return new OkObjectResult(collection);
        }

        
    }
}
