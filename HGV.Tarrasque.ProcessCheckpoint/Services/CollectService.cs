using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Services
{
    public interface ICollectService
    {    
        Task Collect(TextReader reader, TextWriter writer,IAsyncCollector<Match> queue, ILogger log);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient apiClient;

        public CollectService(IDotaApiClient client)
        {
            this.apiClient = client;
        }

        public async Task Collect(TextReader reader, TextWriter writer, IAsyncCollector<Match> queue, ILogger log)
        {
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();
            Guard.Argument(queue, nameof(queue)).NotNull();

            var input = await reader.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<CheckpointModel>(input);

            var matches = await TryGetMatches(checkpoint.Latest);

            checkpoint.Batch = matches.Count();

            var split = DateTimeOffset.UtcNow - matches.Max(_ => _.GetEnd());
            checkpoint.Delta = split.ToString("c");

            var max = matches.Max(_ => _.match_seq_num);
            checkpoint.Latest = max + 1;

            foreach (var match in matches)
            {
                if(match.game_mode == 18)
                {
                    checkpoint.Total++;
                    await queue.AddAsync(match);
                    break;
                }
            }

            var output = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(output);

            var timeout = (int)Math.Round(((100.0f - checkpoint.Batch) / 100.0f) * 15.0f);
            await Task.Delay(TimeSpan.FromSeconds(timeout));
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
