﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Entities
{
    public interface IMatchesCounter
    {
        void Add(int amount);
        Task Reset();
        void Delete();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MatchCounter : IMatchesCounter
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        public void Add(int amount)
        {
            this.Value += amount;
        }

        public Task Reset()
        {
            this.Value = 0;
            return Task.CompletedTask;
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }

        [FunctionName(nameof(MatchCounter))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<MatchCounter>();
    }
}
