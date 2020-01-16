using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public interface ICounter
    {
        void Add(int amount);
        Task Reset();
        Task<int> Get();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Counter : ICounter
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        public void Add(int amount)
        {
            this.Value += amount;
        }

        public Task Reset()
        {
            this.Value = 0;
            return Task.CompletedTask;
        }

        public Task<int> Get()
        {
            return Task.FromResult(this.Value);
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }
    }
}
