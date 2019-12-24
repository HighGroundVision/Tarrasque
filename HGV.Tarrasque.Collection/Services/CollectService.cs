using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Collection.Extensions;
using HGV.Tarrasque.Collection.Models;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Collection.Services
{
    public interface ICollectService
    {    
        Task Collect(TextReader readerCheckpoint, TextWriter writerCheckpoint, TextReader readerHistory, TextWriter writerHistory, IAsyncCollector<MatchReference> queue);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public CollectService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = new MetaClient();
        }

        public async Task Collect(TextReader readerCheckpoint, TextWriter writerCheckpoint, TextReader readerHistory, TextWriter writerHistory, IAsyncCollector<MatchReference> queue)
        {
            var checkpoint = await ReadItem<Checkpoint>(readerCheckpoint);
            var history = await ReadItem<History>(readerHistory);

            var matches = await TryGetMatches(checkpoint.Latest);

            var collection = matches
                .Where(_ => _.game_mode == 18)
                .Where(_ => _.GetDuration().TotalMinutes > 15)
                .ToList();

            checkpoint.Split.Min = DateTimeOffset.UtcNow - matches.Min(_ => _.GetStart());
            checkpoint.Split.Max = DateTimeOffset.UtcNow - matches.Max(_ => _.GetEnd());
            checkpoint.Latest = matches.Max(_ => _.match_seq_num);

            history.TotalMatches += matches.Count;
            foreach (var item in collection)
            {
                if (history.Matches.Contains(item.match_id) == false)
                {
                    history.AddHistory(item.match_id);

                    var obj = new MatchReference()
                    {
                        Match = item.match_id,
                        Date = item.GetStart().ToString("yy-MM-dd"),
                        Region = this.metaClient.ConvertClusterToRegion(item.cluster)
                    };
                    await queue.AddAsync(obj);
                }
            }

            await WriteItem(writerCheckpoint, checkpoint);
            await WriteItem(writerHistory, history);
        }

        private static async Task WriteItem<T>(TextWriter writer, T obj)
        {
            var output = JsonConvert.SerializeObject(obj);
            await writer.WriteAsync(output);
        }

        private static async Task<T> ReadItem<T>(TextReader reader)
        {
            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(input))
                throw new JsonSerializationException("Document is Null");

            var obj = JsonConvert.DeserializeObject<T>(input);
            return obj;
        }

        private static async Task ReadUpdateWriteHandler<T>(TextReader reader, TextWriter writer, Action<T> update) where T : class
        {
            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(input))
                throw new JsonSerializationException("Document is Null");
                
            var data = JsonConvert.DeserializeObject<T>(input);

            update(data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private async Task<List<Match>> TryGetMatches(long latest)
        {
            var policy = Policy
                 .Handle<Exception>()
                 .WaitAndRetryAsync(new[]
                 {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                 });

            var collection = await policy.ExecuteAsync<List<Match>>(async () =>
            {
                var matches = await this.apiClient.GetMatchesInSequence(latest);

                if (matches.Count == 0)
                    throw new ApplicationException("No Matches Returned from API");

                return matches;
            });

            return collection;
        }
    }
}
