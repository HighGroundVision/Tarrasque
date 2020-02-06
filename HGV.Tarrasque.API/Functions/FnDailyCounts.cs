using HGV.Basilius;
using HGV.Tarrasque.API.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Functions
{
    public class FnDailyCounts
    {
        [FunctionName("FnDailyModes")]
        public async Task DailyModes(
            [TimerTrigger("0 0 7 * * *")]TimerInfo myTimer,
            [DurableClient] IDurableEntityClient client,
            IBinder binder,
            ILogger log
        )
        {
            try
            {
                var last = DateTime.UtcNow.AddDays(-1);
                var timestamp = last.ToString("yyMMdd");
                var data = new Dictionary<int, int>();
                var modes = MetaClient.Instance.Value.GetModes();

                foreach (var item in modes)
                {
                    var key = $"{item.Key}|{timestamp}";
                    var id = new EntityId(nameof(ModeEntity), key);

                    var entity = await client.ReadEntityStateAsync<ModeEntity>(id);
                    var value = entity.EntityState?.Total ?? 0;
                    data.Add(item.Key, value);

                    await client.SignalEntityAsync<IModeEntity>(id, proxy => proxy.Delete());
                }

                var attr = new BlobAttribute($"hgv-modes/{timestamp}.json");
                var writer = await binder.BindAsync<TextWriter>(attr);
                var json = JsonConvert.SerializeObject(data);
                await writer.WriteAsync(json);
            }
            catch(Exception ex)
            {
                throw;
            }
        }

    }
}
