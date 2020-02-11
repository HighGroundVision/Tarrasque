using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public interface IHeroEntity
    {
        Task IncrementTotal();
        Task IncrementWins();
        Task Reset();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class HeroEntity : IHeroEntity
    {
        [JsonProperty(nameof(Total))]
        public int Total { get; set; }

        [JsonProperty(nameof(Wins))]
        public int Wins { get; set; }

        public Task IncrementTotal()
        {
            this.Total++;
            return Task.CompletedTask;
        }

        public Task IncrementWins()
        {
            this.Wins++;
            return Task.CompletedTask;
        }

        public Task Reset()
        {
            this.Total = 0;
            this.Wins = 0;
            return Task.CompletedTask;
        }

        [FunctionName(nameof(HeroEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<HeroEntity>();

      
    }
}
