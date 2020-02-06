using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Entities
{
    public interface IModeEntity
    {
        void Add(int amount);
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ModeEntity : IModeEntity
    {
        [JsonProperty(nameof(Total))]
        public int Total { get; set; }

        public void Add(int amount)
        {
            this.Total += amount;
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }

        [FunctionName(nameof(ModeEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<ModeEntity>();
    }
}
