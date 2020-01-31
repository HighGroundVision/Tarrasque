using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Entities
{
    public class AbilityPairData
    {
        public int AbilityId { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }

    public interface IAbilityPairEntity
    {
        void AddWin(List<int> abilities);
        void AddLoss(List<int> abilities);
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AbilityPairEntity : IAbilityPairEntity
    {
        public AbilityPairEntity()
        {
            this.Collection = new List<AbilityPairData>();
        }

        [JsonProperty("collection")]
        public List<AbilityPairData> Collection { get; set; }

        public void AddWin(List<int> abilities)
        {
            foreach (var id in abilities)
            {
                var existing = this.Collection.Find(_ => _.AbilityId == id);
                if (existing == null)
                {
                    this.Collection.Add(new AbilityPairData() { AbilityId = id, Total = 1, Wins = 1, Losses = 0 });
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
                this.Collection = new List<AbilityPairData>();

            foreach (var id in abilities)
            {
                var existing = this.Collection.Find(_ => _.AbilityId == id);
                if (existing == null)
                {
                    this.Collection.Add(new AbilityPairData() { AbilityId = id, Total = 1, Wins = 1, Losses = 0 });
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

        [FunctionName(nameof(AbilityPairEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<AbilityPairEntity>();
    }
}
