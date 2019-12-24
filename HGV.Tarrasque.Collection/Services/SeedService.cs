using HGV.Daedalus;
using HGV.Tarrasque.Collection.Models;
using HGV.Tarrasque.Collection.Extensions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Collection.Services
{
    public interface ISeedService
    {
        Task SeedCheckpoint(TextWriter writer);
        Task SeedHistory(TextWriter writer);
    }

    public class SeedService : ISeedService
    {
        private readonly IDotaApiClient client;

        public SeedService(IDotaApiClient client)
        {
            this.client = client;
        }

        public async Task SeedCheckpoint(TextWriter writer)
        {
            var checkpoint = new Models.Checkpoint();

            // Error Trap - Polly
            var matches = await this.client.GetLastestMatches();

            checkpoint.Latest = matches.Max(_ => _.match_seq_num);

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);
        }

        public async Task SeedHistory(TextWriter writer)
        {
            var checkpoint = new Models.History();

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);

        }
    }
}
