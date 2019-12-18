using HGV.Daedalus;
using HGV.Tarrasque.Collection.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Collection.Services
{
    public interface ICollectService
    {    
        Task Collect(TextReader reader, TextWriter writer, IAsyncCollector<Models.Match> queue, ILogger log);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient client;

        public CollectService(IDotaApiClient client)
        {
            this.client = client;
        }

        public async Task Collect(TextReader reader, TextWriter writer, IAsyncCollector<Models.Match> queue, ILogger log)
        {
            var input = await reader.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<Models.Checkpoint>(input);

            // Error Trap - Polly
            var matches = await this.client.GetMatchesInSequence(checkpoint.Latest);
            checkpoint.Latest = matches.Max(_ => _.match_seq_num);

            var min = matches.Min(_ => _.start_time);
            var max = matches.Max(_ => _.start_time);
            checkpoint.Timestamp.SetRange(min, max);
            checkpoint.Timestamp.ToLocal();

            var collection = matches
                .Where(_ => _.game_mode == 18)
                .Where(_ => DateTimeOffset.FromUnixTimeSeconds(_.duration).TimeOfDay.TotalMinutes > 15)
                .Select(_ => _.match_id)
                .ToList();

            foreach (var id in collection)
            {
                if(checkpoint.History.Contains(id) == false)
                {
                    checkpoint.AddHistory(id);

                    var item = new Models.Match() { Id = id };
                    await queue.AddAsync(item);
                }
            }

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);
        }
    }
}
