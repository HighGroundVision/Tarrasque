
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using HGV.Tarrasque.Models;
using System.Collections.Generic;

namespace HGV.Tarrasque.Functions
{
    public static class FnVote
    {
        [FunctionName("Vote")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,
            [Queue("hgv-vote")]IAsyncCollector<VoteData> queue,
            TraceWriter log
        )
        {
            var json = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return new BadRequestObjectResult("Please supply a Vody.");

            var data = JsonConvert.DeserializeObject<VoteData>(json);
            if (data == null)
                return new BadRequestObjectResult("Please format the Body correctly.");

            var typeGruad = new List<int>() { 1, 2 };
            if (typeGruad.Contains(data.type) == false) 
                return new BadRequestObjectResult("Please supply an valid Type. 1 for ability and 2 for combos.");

            if (data.account_id == 0)
                return new BadRequestObjectResult("Please supply an valid AccountId.");

            if (string.IsNullOrWhiteSpace(data.key))
                return new BadRequestObjectResult("Please supply an valid Ability/Combo key.");

            await queue.AddAsync(data);

            return new OkResult();
        }
    }
}
