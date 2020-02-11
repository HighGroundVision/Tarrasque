using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public interface ITalenentEntity
    {
        Task Reset();
        Task IncrementTotal();
        Task IncrementWins();
        Task<int> GetTotal();
        Task<int> GetWins();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TalenentEntity : ITalenentEntity
    {
        [JsonProperty(nameof(Total))]
        public int Total { get; set; }

        [JsonProperty(nameof(Wins))]
        public int Wins { get; set; }

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

        public Task IncrementWins()
        {
            this.Wins++;
            return Task.CompletedTask;
        }

        public Task<int> GetWins()
        {
            return Task.FromResult(this.Wins);
        }

        [FunctionName(nameof(TalenentEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<TalenentEntity>();
    }
}
