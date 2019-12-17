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
            var history = matches.Select(_ => _.match_seq_num).ToList();

            var model = new Models.Checkpoint()
            {
                Timestamp = DateTime.UtcNow,
                Latest = history.Max(),
                History = history,
                Counter = 1,
            };

            var output = JsonConvert.SerializeObject(model);
            await writer.WriteAsync(output);
        }

        public async Task CollectMatches(TextReader reader, TextWriter writer)
        {
            var input = await reader.ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<Models.Checkpoint>(input);

            model.Timestamp = DateTime.UtcNow;
            model.Counter++;

            var output = JsonConvert.SerializeObject(model);
            await writer.WriteAsync(output);
        }
    }
}
