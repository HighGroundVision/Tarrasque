using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessCheckpoint.Entities;
using HGV.Tarrasque.ProcessCheckpoint.Exceptions;
using Humanizer;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
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
    public interface ICollectService
    {
        Task ProcessCheckpoint(TextReader reader, TextWriter writer, CloudQueue queue, IDurableClient client, ILogger log);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient apiClient;
        private readonly DurableCircuitBreakerClient circuitBreaker;

        public CollectService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.circuitBreaker = new DurableCircuitBreakerClient("cb283EFF96DC4646349FCE26D6608B63C5");
        }

        public async Task ProcessCheckpoint(TextReader reader, TextWriter writer, CloudQueue queue, IDurableClient client, ILogger log)
        {
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();
            Guard.Argument(queue, nameof(queue)).NotNull();
            Guard.Argument(client, nameof(client)).NotNull();
            Guard.Argument(log, nameof(log)).NotNull();
            
            var checkpoint = await GetCheckpoint(reader);
            try
            {
                await Collection(checkpoint, queue, client, log);
            }
            catch (ExecutionProhibitedException)
            {
                await CircuitBroken(log);
            }
            catch (Exception ex)
            {
                await LogFailire(client, log, ex);
            }
            finally
            {
                await NextCheckpoint(checkpoint, writer);
            }
        }

        private async Task<CheckpointModel> GetCheckpoint(TextReader readerCheckpoint)
        {
            Guard.Argument(readerCheckpoint, nameof(readerCheckpoint)).NotNull();

            var json = await readerCheckpoint.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<CheckpointModel>(json);
            return checkpoint;
        }

        private async Task Collection(CheckpointModel checkpoint, CloudQueue queue, IDurableClient client, ILogger log)
        {
            Guard.Argument(checkpoint, nameof(checkpoint)).NotNull();
            Guard.Argument(queue, nameof(queue)).NotNull();
            Guard.Argument(client, nameof(client)).NotNull();
            Guard.Argument(log, nameof(log)).NotNull();

            var isPermitted = await this.circuitBreaker.IsExecutionPermitted(client, log);
            if (isPermitted == false)
                throw new ExecutionProhibitedException();

            var matches = await this.Collect(checkpoint, queue, log);
            foreach (var item in matches)
            {
                var json = JsonConvert.SerializeObject(item);
                var msg = new CloudQueueMessage(json);
                await queue.AddMessageAsync(msg);
            }

            await this.circuitBreaker.RecordSuccess(client, log);

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        private async Task CircuitBroken(ILogger log)
        {
            Guard.Argument(log, nameof(log)).NotNull();

            log.LogWarning("CircuitBreaker: Open");

            await Task.Delay(TimeSpan.FromMinutes(1));
        }

        private async Task LogFailire(IDurableClient client, ILogger log, Exception ex)
        {
            Guard.Argument(client, nameof(client)).NotNull();
            Guard.Argument(log, nameof(log)).NotNull();
            Guard.Argument(ex, nameof(ex)).NotNull();

            log.LogError(ex.Message);

            await this.circuitBreaker.RecordFailure(client, log);
        }

        private async Task NextCheckpoint(CheckpointModel checkpoint, TextWriter writer)
        {
            Guard.Argument(checkpoint, nameof(checkpoint)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var json = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(json);
        }

        private async Task<List<Match>> Collect(CheckpointModel checkpoint, CloudQueue queue, ILogger log)
        {
            Guard.Argument(checkpoint, nameof(checkpoint)).NotNull();
            Guard.Argument(queue, nameof(queue)).NotNull();
            Guard.Argument(log, nameof(log)).NotNull();

            // Get Data
            await queue.FetchAttributesAsync();
            var matches = await GetMatches(checkpoint.Latest, log);
            var collection = matches.Where(_ => _.game_mode == 18).ToList();

            // Update Checkpoint
            checkpoint.Total += collection.Count;
            checkpoint.Batch = matches.Count();
            checkpoint.Delta = (DateTimeOffset.UtcNow - matches.Max(_ => _.GetEnd())).Humanize(2);
            checkpoint.Latest = matches.Max(_ => _.match_seq_num) + 1;
            checkpoint.InQueue = queue.ApproximateMessageCount.GetValueOrDefault();

            return collection;
        }

        private async Task<List<Match>> GetMatches(ulong latest, ILogger log)
        {
            Guard.Argument(latest, nameof(latest)).NotDefault();
            Guard.Argument(log, nameof(log)).NotNull();

            var policy = Policy
                .Handle<BelowLimitException>()
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(10),
                    (ex, timeout) => log.LogDebug(ex.Message)
                );

            var collection = await policy.ExecuteAsync<List<Match>>(async () => 
            {
                var matches = await this.apiClient.GetMatchesInSequence(latest);
                if (matches.Count < 100)
                    throw new BelowLimitException();
                else
                    return matches;
            });

            return collection;
        }
    }
}
