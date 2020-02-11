using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch
{
    public class FnSummaryTrigger
    {
        public FnSummaryTrigger()
        {
        }

        [FunctionName("FnSummaryTrigger")]
        public async Task ProcessAsync(
           [TimerTrigger("0 0 0 * * *")]TimerInfo myTimer,
           [Queue("hgv-summary-regions")]IAsyncCollector<string> queueRegions,
           [Queue("hgv-summary-heroes")]IAsyncCollector<string> queueHeroes,
           ILogger log)
        {
            var timestamp = DateTime.UtcNow.ToString("yy-MM-dd");

            await queueRegions.AddAsync(timestamp);
            await queueHeroes.AddAsync(timestamp);
        }
    }
}
