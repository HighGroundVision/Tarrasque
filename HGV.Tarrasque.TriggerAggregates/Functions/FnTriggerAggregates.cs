using System;
using HGV.Basilius;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.TriggerAggregates
{
    public static class FnTriggerAggregates
    {
        [FunctionName("FnTriggerAggregates")]
        public static void Run(
            [TimerTrigger("0 0 0 * * *")]TimerInfo timer, // at 12:00 AM every day
            [Queue("hgv-aggregates")]IAsyncCollector<object> queue,
            ILogger log
        )
        {
            var client = MetaClient.Instance.Value;
            var regions = client.GetRegions();
            foreach (var region in regions)
            {
                var id = region.Key;
            }
        }
    }
}
