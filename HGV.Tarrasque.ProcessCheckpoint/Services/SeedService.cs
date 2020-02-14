using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchHistory;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Services
{
    public interface ISeedService
    {
        Task SeedCheckpoint(TextWriter writer);
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
            Guard.Argument(writer, nameof(writer)).NotNull();

            var checkpoint = new CheckpointModel();

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                });

            var matches = await policy.ExecuteAsync<List<Match>>(async () =>
            {
                return await this.client.GetLastestMatches();
            });

            checkpoint.Latest = matches.Max(_ => _.match_seq_num);

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);
        }
    }
}
