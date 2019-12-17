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
                History = history,
                Counter = 1,
            };

            var json = JsonConvert.SerializeObject(model);
            await writer.WriteAsync(json);
        }

        public async Task CollectMatches(TextReader reader, TextWriter writer)
        {
            var json = await reader.ReadToEndAsync();


            await writer.WriteAsync(json);

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
