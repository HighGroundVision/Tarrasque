using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Services
{
    public interface ICollectService
    {    
        Task Collect(TextReader readerCheckpoint, TextWriter writerCheckpoint, TextReader readerHistory, TextWriter writerHistory, IAsyncCollector<MatchReference> queue);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient apiClient;

        public CollectService(IDotaApiClient client)
        {
            this.apiClient = client;
        }

        public async Task Collect(TextReader readerCheckpoint, TextWriter writerCheckpoint, TextReader readerHistory, TextWriter writerHistory, IAsyncCollector<MatchReference> queue)
        {
            Guard.Argument(readerCheckpoint, nameof(readerCheckpoint)).NotNull();
            Guard.Argument(writerCheckpoint, nameof(writerCheckpoint)).NotNull();
            Guard.Argument(readerHistory, nameof(readerHistory)).NotNull();
            Guard.Argument(writerHistory, nameof(writerHistory)).NotNull();
            Guard.Argument(queue, nameof(queue)).NotNull();

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

                    await queue.AddAsync(new MatchReference(item));
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

        private async Task<List<Match>> TryGetMatches(ulong latest)
        {
            var policy = Policy
                 .Handle<Exception>()
                 .WaitAndRetryAsync(new[]
                 {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
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
