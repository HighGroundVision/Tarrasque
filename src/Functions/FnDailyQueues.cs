using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace HGV.Tarrasque.Functions
{
    public static class FnDailyQueues
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("FnDailyQueues")]
        public static void Run(
            // Timer (once/day at 00:00)
            [TimerTrigger("0 0 * * *")]TimerInfo myTimer,
            // Output Queues
            [Queue("hgv-ad-stats-abilities")]ICollector<string> queueStatsADAbilities,
            [Queue("hgv-ad-stats-combos")]ICollector<string> queueStatsADCombos,
            [Queue("hgv-ad-stats-drafts")]ICollector<string> queueStatsADDrafts,
            // Logger
            TraceWriter log
            )
        {
            log.Info($"FnDailyQueues start on: {DateTime.UtcNow}");

            // Get Yesterday
            var day = DateTime.UtcNow.AddDays(-1).DayOfYear.ToString();

            // Queues
            queueStatsADAbilities.Add(day);
            //queueStatsADCombos = day;
            //queueStatsADDrafts = day;
        }
    }
}
