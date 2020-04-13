using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchHistory;
using HGV.Tarrasque.ProcessCheckpoint.Models;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Services
{
    public interface ICheckPointService
    {
        Task StartCheckpoint(TextReader reader, TextWriter writer, bool reset, ILogger log);
        Task<CheckpointStatus> CheckpointStatus(TextReader reader, Dictionary<string, CloudQueue> queues, ILogger log);
    }

    public class CheckPointService : ICheckPointService
    {
        private readonly IDotaApiClient client;

        public CheckPointService(IDotaApiClient client)
        {
            this.client = client;
        }

        private async Task SeedCheckpoint(TextWriter writer, ILogger log)
        {
            Guard.Argument(writer, nameof(writer)).NotNull();

            var checkpoint = new Checkpoint();

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

        public async Task StartCheckpoint(TextReader reader, TextWriter writer, bool reset, ILogger log)
        {
            if (reader == null || reset == true)
            {
                await this.SeedCheckpoint(writer, log);
            }
            else
            {
                var json = await reader.ReadToEndAsync();
                await writer.WriteAsync(json);
            }
        }

        public async Task<CheckpointStatus> CheckpointStatus(TextReader reader, Dictionary<string, CloudQueue> queues, ILogger log)
        {
            var json = await reader.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<Checkpoint>(json);

            var status = new CheckpointStatus();
            status.TotalAllMatches = checkpoint.Total;
            status.TotalADMatches = checkpoint.ADTotal;
            status.Delta = (DateTimeOffset.UtcNow - checkpoint.Timestamp).Humanize(3);

            foreach (var item in queues)
            {
                await item.Value.FetchAttributesAsync();
                status.Queues.Add(item.Key, item.Value.ApproximateMessageCount.GetValueOrDefault());
            }

            return status;
        }
    }
}
