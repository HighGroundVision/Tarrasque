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
        void AddWin(List<int> abilities);
        void AddLoss(List<int> abilities);
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TalentEntity : ITalentEntity
    {
        public TalentEntity()
        {
            this.Collection = new List<TalentData>();
        }

        [JsonProperty("collection")]
        public List<TalentData> Collection { get; set; }

 
        public void AddWin(List<int> abilities)
        {
            if (this.Collection == null)
                this.Collection = new List<TalentData>();

            foreach (var id in abilities)
            {
                var existing = this.Collection.Find(_ => _.TalentId == id);
                if (existing == null)
                {
                    this.Collection.Add(new TalentData() { TalentId = id, Total = 1, Wins = 1, Losses = 0 });
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
                this.Collection = new List<TalentData>();

            foreach (var id in abilities)
            {
                var existing = this.Collection.Find(_ => _.TalentId == id);
                if (existing == null)
                {
                    this.Collection.Add(new TalentData() { TalentId = id, Total = 1, Wins = 1, Losses = 0 });
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

        [FunctionName(nameof(TalentEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<TalentEntity>();
    }
}
