using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Entities
{
    public interface IRegionsCounter
    {
        void Increment();
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class RegionsCounter : IRegionsCounter
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        public void Increment()
        {
            this.Value++;
        }

        public Task Reset()
        {
            this.Value = 0;
            return Task.CompletedTask;
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }

        [FunctionName(nameof(RegionsCounter))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<RegionsCounter>();
    }
}
