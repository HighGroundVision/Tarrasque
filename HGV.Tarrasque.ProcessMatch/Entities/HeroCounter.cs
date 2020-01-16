using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public interface IHeroCounter
    {
        void AddWin();
        void AddLoss();
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class HeroCounter : IHeroCounter
    {
        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("losses")]
        public int Losses { get; set; }

        public void AddWin()
        {
            this.Wins++;
        }

        public void AddLoss()
        {
            this.Losses++;
        }

        public Task Reset()
        {
            this.Wins = 0;
            this.Losses = 0;
            return Task.CompletedTask;
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }

        [FunctionName(nameof(HeroCounter))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<HeroCounter>();
    }
}
