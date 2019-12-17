using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HGV.Daedalus;

namespace HGV.Tarrasque.Collection
{
    public interface IMyService
    {
        Task Seed(TextWriter writer);
        Task CollectMatches(TextReader reader, TextWriter writer);
    }

    public class MyService : IMyService
    {
        private readonly IDotaApiClient client;

        public MyService(IDotaApiClient client)
        {
            this.client = client;
        }

        public async Task Seed(TextWriter writer)
        {
            var matches = await this.client.GetLastestMatches();

            var checkpoint = new Models.Checkpoint();

            var min = matches.Min(_ => _.start_time);
            var max = matches.Max(_ => _.start_time);
            checkpoint.Timestamp.SetRange(min, max);
            checkpoint.Timestamp.ToLocal();

            var history = matches.Select(_ => _.match_seq_num).ToList();
            checkpoint.Latest = history.Max();
            checkpoint.History.AddRange(history);

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);
        }

        public async Task CollectMatches(TextReader reader, TextWriter writer)
        {
            var input = await reader.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<Models.Checkpoint>(input);

            var matches = await this.client.GetMatchesInSequence(checkpoint.Latest);
            
            foreach (var match in matches)
            {
                if(checkpoint.History.Contains(match.match_seq_num))
                    continue;
            }

            checkpoint.Latest = checkpoint.History.Max();

            var min = matches.Min(_ => _.start_time);
            var max = matches.Max(_ => _.start_time);
            checkpoint.Timestamp.SetRange(min, max);
            checkpoint.Timestamp.ToLocal();

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);
        }
    }
}
