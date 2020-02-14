using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Exceptions;
using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessCheckpoint.Entities;
using HGV.Tarrasque.ProcessCheckpoint.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Functions
{
    public class FnCheckpoint
    {
        private readonly ICollectService _service;
        private readonly DurableCircuitBreakerClient _circuitBreaker;

        public FnCheckpoint(ICollectService service)
        {
            _service = service;
            _circuitBreaker = new DurableCircuitBreakerClient("cb283EFF96DC4646349FCE26D6608B63C5");
        }

        [FunctionName("FnCheckpoint")]
        public async Task Checkpoint(
            [BlobTrigger("hgv-checkpoint/master.json")]TextReader reader,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            [Queue("hgv-ad-matches")]IAsyncCollector<Match> queue,
            [DurableClient]IDurableClient client,
            ILogger log)
        {
            using (new Timer("FnCheckpoint", log))
            {
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
        }

        private static async Task<CheckpointModel> GetCheckpoint(TextReader readerCheckpoint)
        {
            var json = await readerCheckpoint.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<CheckpointModel>(json);
            return checkpoint;
        }

        private async Task Collection(CheckpointModel checkpoint, 
            IAsyncCollector<Match> queueMatches,
            IDurableClient client, 
            ILogger log
        )
        {
            var isPermitted = await _circuitBreaker.IsExecutionPermitted(client, log);
            if (isPermitted == false)
                throw new ExecutionProhibitedException();

            var matches = await _service.Collect(checkpoint, log);
            foreach (var item in matches)
                await queueMatches.AddAsync(item);

            await _circuitBreaker.RecordSuccess(client, log);

            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        private async Task CircuitBroken(ILogger log)
        {
            log.LogWarning("CircuitBreaker: Broken");

            await Task.Delay(TimeSpan.FromMinutes(5));
        }

        private async Task LogFailire(IDurableClient client, ILogger log, Exception ex)
        {
            log.LogError(ex.Message);

            await _circuitBreaker.RecordFailure(client, log);
        }

        private async Task NextCheckpoint(CheckpointModel checkpoint, TextWriter writer)
        {
            var json = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(json);
        }

    }
}
