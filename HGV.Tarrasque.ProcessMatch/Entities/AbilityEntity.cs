using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public interface IAbilityEntity
    {
        Task Reset();
        Task IncrementTotal();
        Task<int> GetTotal();
        Task IncrementWins();
        Task<int> GetWins();
        Task AddPriority(int amount);
        public Task<int> GetPriority();
        Task IncrementAncestry();
        Task<int> GetAncestry();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AbilityEntity : IAbilityEntity
    {
        [JsonProperty(nameof(Total))]
        public int Total { get; set; }

        [JsonProperty(nameof(Wins))]
        public int Wins { get; set; }

        [JsonProperty(nameof(Ancestry))]
        public int Ancestry { get; set; }

        [JsonProperty(nameof(Priority))]
        public int Priority { get; set; }

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

        public Task AddPriority(int amount)
        {
            this.Priority += amount;
            return Task.CompletedTask;
        }

        public Task<int> GetPriority()
        {
            return Task.FromResult(this.Priority);
        }

        public Task IncrementAncestry()
        {
            this.Ancestry++;
            return Task.CompletedTask;
        }

        public Task<int> GetAncestry()
        {
            return Task.FromResult(this.Ancestry);
        }

        [FunctionName(nameof(AbilityEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<AbilityEntity>();
    }
}
