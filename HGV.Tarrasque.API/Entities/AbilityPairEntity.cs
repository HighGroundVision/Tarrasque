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
        void Increment(List<int> abilities, bool victory);
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AbilityPairEntity : IAbilityPairEntity
    {
        [JsonProperty("collection")]
        public List<AbilityPairData> Collection { get; set; }

        public void Increment(List<int> abilities, bool victory)
        {
            foreach (var id in abilities)
            {
                var existing = Collection.Find(_ => _.AbilityId == id);
                if (existing == null)
                {
                    if (victory)
                        Collection.Add(new AbilityPairData() { AbilityId = id, Total = 1, Wins = 1, Losses = 0 });
                    else
                        Collection.Add(new AbilityPairData() { AbilityId = id, Total = 1, Wins = 0, Losses = 1 });
                }
                else
                {
                    existing.Total++;
                    if (victory)
                        existing.Wins++;
                    else
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
