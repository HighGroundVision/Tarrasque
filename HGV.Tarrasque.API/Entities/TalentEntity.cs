using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Entities
{
    public class TalentData
    {
        public int TalentId { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }

    public interface ITalentEntity
    {
        void Increment(List<int> talents, bool victory);
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TalentEntity : ITalentEntity
    {
        [JsonProperty("collection")]
        public List<TalentData> Collection { get; set; }


        public void Increment(List<int> talents, bool victory)
        {
            foreach (var id in talents)
            {
                var existing = Collection.Find(_ => _.TalentId == id);
                if (existing == null)
                {
                    if (victory)
                        Collection.Add(new TalentData() { TalentId = id, Total = 1, Wins = 1, Losses = 0 });
                    else
                        Collection.Add(new TalentData() { TalentId = id, Total = 1, Wins = 0, Losses = 1 });
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

        [FunctionName(nameof(TalentEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<TalentEntity>();
    }
}
