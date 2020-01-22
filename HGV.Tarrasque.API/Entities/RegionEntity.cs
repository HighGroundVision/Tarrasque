using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Entities
{
    public interface IRegionEntity
    {
        void Increment();
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class RegionEntity : IRegionEntity
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        public void Increment()
        {
            this.Total++;
        }

        public Task Reset()
        {
            this.Total = 0;
            return Task.CompletedTask;
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }

        [FunctionName(nameof(RegionEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<RegionEntity>();
    }
}
