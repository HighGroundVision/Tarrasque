using HGV.Basilius;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessMatch.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch
{
    public class FnSummaryRegion
    {
        public FnSummaryRegion()
        {
        }

        [FunctionName("FnSummaryRegion")]
        public async Task Process(
            [QueueTrigger("hgv-summary-regions")]string timestamp,
            [DurableClient]IDurableClient client,
            IBinder binder,
            ILogger log)
        {
            var regions = MetaClient.Instance.Value.GetRegions()
                .OrderBy(_ => _.Key)
                .ToList();

            foreach (var region in regions)
            {
                var id = new EntityId(nameof(RegionEntity), $"{region.Key}");
                var state = await client.ReadEntityStateAsync<RegionEntity>(id);
                await client.SignalEntityAsync<IRegionEntity>(id, _ => _.Reset());

                var model = new RegionModel()
                {
                    RegionId = region.Key,
                    RegionName = region.Value,
                    Timestamp = timestamp,
                    Total = state.EntityState?.Total ?? 0,
                };

                var path = $"hgv-regions/{timestamp}/{region.Key:00}.json";
                var attr = new BlobAttribute(path);
                var writer = await binder.BindAsync<TextWriter>(attr);
                var json = JsonConvert.SerializeObject(model);
                await writer.WriteAsync(json);
            }
        }
    }
}