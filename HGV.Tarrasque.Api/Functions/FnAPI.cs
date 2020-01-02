using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using HGV.Daedalus;

namespace HGV.Tarrasque.Api.Functions
{
    public class FnAPI
    {
        private readonly IDotaApiClient apiClient;

        public FnAPI(IDotaApiClient client)
        {
            apiClient = client;
        }

        [FunctionName("FnTimeline")]
        public async Task<IActionResult> GetTimeline(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timelime/{id:int}")] HttpRequest req,
            [Blob("hgv-regions/{id}.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            return new OkObjectResult(
                await reader.ReadToEndAsync()
            );
        }

        [FunctionName("FnHeroes")]
        public async Task<IActionResult> GetHeroes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "heroes/{region}/{date}")] HttpRequest req,
            [Blob("hgv-heroes/{region}/{date}/heroes.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            return new OkObjectResult(
                await reader.ReadToEndAsync()
            );
        }

        [FunctionName("FnAbilities")]
        public async Task<IActionResult> GetAbilities(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "abilities/{region}/{date}")] HttpRequest req,
            [Blob("hgv-abilities/{region}/{date}/abilities.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            return new OkObjectResult(
                await reader.ReadToEndAsync()
            );
        }

        [FunctionName("FnPlayer")]
        public async Task<IActionResult> GetPlayer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "player/{account}")] HttpRequest req,
            [Blob("hgv-accounts/{account}.json")]TextReader reader,
            ILogger log)
        {
            if (reader == null)
                return new NotFoundResult();

            return new OkObjectResult(
                await reader.ReadToEndAsync()
            );
        }
    }
}
