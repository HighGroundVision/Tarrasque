using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Functions
{
    public class FnHeroesAPI
    {
        private readonly IReadOnlyPolicyRegistry<string> policyRegistry;
        private readonly IHeroService heroService;

        public FnHeroesAPI(IReadOnlyPolicyRegistry<string> policyRegistry, IHeroService heroService)
        {
            this.policyRegistry = policyRegistry;
            this.heroService = heroService;
        }

        [FunctionName("FnHeroesCategories")]
        public IActionResult GetHeroesCategories(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "heroes/categories")] HttpRequest req,
            ILogger log)
        {
            var collection = heroService.GetHeroCategories();
            return new OkObjectResult(collection);
        }

        [FunctionName("FnHeroesHistory")]
        public async Task<IActionResult> GetHeroesHistory(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "heroes/history")] HttpRequest req,
           [Table("HGVHeroes")]CloudTable table,
           ILogger log)
        {
            var start = DateTime.UtcNow.AddDays(-1).ToString("yy-MM-dd");
            var end = DateTime.UtcNow.AddDays(-5).ToString("yy-MM-dd");

            var cachePolicy = policyRegistry.Get<IAsyncPolicy<List<HeroHistory>>>("FnHeroesHistory");
            var collection = await cachePolicy.ExecuteAsync(
                context => heroService.GetHeroHistory(start, end, table, log), 
                new Context("FnHeroesHistory")
            );

            return new OkObjectResult(collection);
        }

        [FunctionName("FnHeroDetails")]
        public async Task<IActionResult> GetHeroDetails(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hero/{id}")] HttpRequest req,
           int id,
           IBinder binder,
           ILogger log)
        {
            var model = await this.heroService.GetHeroDetails(id, binder, log);
            return new OkObjectResult(model);
        }
    }
}
