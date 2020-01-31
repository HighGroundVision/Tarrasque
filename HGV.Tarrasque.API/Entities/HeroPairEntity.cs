using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace HGV.Tarrasque.API.Entities
{
    public class HeroPairData
    {
        public int AbilityId { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }

    public interface IHeroPairEntity
    {
        void AddWin(List<int> abilities);
        void AddLoss(List<int> abilities);
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class HeroPairEntity : IHeroPairEntity
    {
        public HeroPairEntity()
        {
            this.Collection = new List<HeroPairData>();
        }

        [JsonProperty("collection")]
        public List<HeroPairData> Collection { get; set; }

        public void AddWin(List<int> abilities)
        {
            if (this.Collection == null)
                this.Collection = new List<HeroPairData>();

            foreach (var id in abilities)
            {
                var existing = this.Collection.Find(_ => _.AbilityId == id);
                if (existing == null)
                {
                    this.Collection.Add(new HeroPairData() { AbilityId = id, Total = 1, Wins = 1, Losses = 0 });
                }
                else
                {
                    existing.Total++;
                    existing.Wins++;
                }
            }
        }

        public void AddLoss(List<int> abilities)
        {
            if (this.Collection == null)
                this.Collection = new List<HeroPairData>();

            foreach (var id in abilities)
            {
                var existing = this.Collection.Find(_ => _.AbilityId == id);
                if (existing == null)
                {
                    this.Collection.Add(new HeroPairData() { AbilityId = id, Total = 1, Wins = 1, Losses = 0 });
                }
                else
                {
                    existing.Total++;
                    existing.Losses++;
                }
            }
        }

        public Task Reset()
        {
            this.Collection.Clear();
            return Task.CompletedTask;
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }

        [FunctionName(nameof(HeroPairEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<HeroPairEntity>();
    }
}
