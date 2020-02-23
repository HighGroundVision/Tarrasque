using HGV.Tarrasque.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Custodian
{
    public class FnCustodianTrigger
    {
        public FnCustodianTrigger()
        {
        }

        [FunctionName("FnCustodianTimedTrigger")]
        public async Task CustodianTimedTrigger(
            [TimerTrigger("0 0 1 * * *")]TimerInfo myTimer,
            [Queue("hgv-ad-custodian")]IAsyncCollector<CustodianModel> queue,
            ILogger log
        )
        {
            var msg = new CustodianModel() { Date = DateTime.UtcNow.AddDays(-7).ToString("yy-MM-dd") };
            await queue.AddAsync(msg);
        }

        [FunctionName("FnCustodianManualTrigger")]
        public async Task<IActionResult> CustodianManualTrigger(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "custodian/trigger/{date}")] HttpRequest req,
          string date,
          [Queue("hgv-ad-custodian")]IAsyncCollector<CustodianModel> queue,
          ILogger log)
        {
            var msg = new CustodianModel() { Date = date };
            await queue.AddAsync(msg);

            return new OkResult();
        }
    }
}
