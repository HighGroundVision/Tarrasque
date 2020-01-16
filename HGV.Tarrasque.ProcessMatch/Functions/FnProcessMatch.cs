using HGV.Tarrasque.ProcessMatch.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using HGV.Tarrasque.ProcessMatch.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace HGV.Tarrasque.ProcessMatch.Functions
{
    public class FnProcessMatch
    {
        private readonly IProcessMatchService _service;

        public FnProcessMatch(IProcessMatchService service)
        {
            _service = service;
        }

        [FunctionName("FnProcessMatch")]
        public async Task Process(
            [QueueTrigger("hgv-ad-matches")]MatchReference item,
            [DurableClient] IDurableEntityClient client,
            ILogger log)
        {
            await _service.ProcessMatch(item, client);
        }

        [FunctionName("FnGetMatches")]
        public async Task<IActionResult> GetMatches(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "counter/match/{type}")] HttpRequest req,
            [DurableClient] IDurableEntityClient client,
            string type,
            ILogger log)
        {
            var entityId = new EntityId(nameof(MatchCounter), type);
            var state = await client.ReadEntityStateAsync<MatchCounter>(entityId);
            var data = new
            {
                Key = nameof(MatchCounter),
                Id = type,
                Exists = state.EntityExists,
                Value = state.EntityExists ? state.EntityState.Value : 0,
            };

            return new OkObjectResult(data);
        }

        [FunctionName("FnGetHeroWins")]
        public async Task<IActionResult> GetHeroWins(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "counter/hero/{heroId}")] HttpRequest req,
            [DurableClient] IDurableEntityClient client,
            string heroId,
            ILogger log)
        {
            var entityId = new EntityId(nameof(HeroCounter), heroId);
            var state = await client.ReadEntityStateAsync<HeroCounter>(entityId);
            var data = new
            {
                Key = nameof(HeroCounter),
                Id = heroId,
                Exists = state.EntityExists,
                Wins = state.EntityExists ? state.EntityState.Wins : 0,
                Losses = state.EntityExists ? state.EntityState.Losses : 0,
            };

            return new OkObjectResult(data);
        }
    }
}