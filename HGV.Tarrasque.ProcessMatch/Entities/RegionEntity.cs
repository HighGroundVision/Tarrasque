using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public interface IRegionEntity
    {
        Task IncrementTotal();
        Task Reset();
        Task<int> GetTotal();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class RegionEntity : IRegionEntity
    {
        [JsonProperty(nameof(Total))]
        public int Total { get; set; }

        public Task Reset()
        {
            this.Total = 0;
            return Task.CompletedTask;
        }

        public Task IncrementTotal()
        {
            this.Total++;
            return Task.CompletedTask;
        }

        public Task<int> GetTotal()
        {
            return Task.FromResult(this.Total);
        }


        [FunctionName(nameof(RegionEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<RegionEntity>();
    }
}
