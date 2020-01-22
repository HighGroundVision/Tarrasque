using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Entities
{
    public interface IAbilityEntity
    {
        void Increment(bool victory);
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AbilityEntity : IAbilityEntity
    {
        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("losses")]
        public int Losses { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        public void Increment(bool victory)
        {
            this.Total++;

            if (victory)
                this.Wins++;
            else
                this.Losses++;

        }

        public Task Reset()
        {
            this.Wins = 0;
            this.Losses = 0;
            this.Total = 0;
            return Task.CompletedTask;
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }

        [FunctionName(nameof(AbilityEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<AbilityEntity>();
    }
}
