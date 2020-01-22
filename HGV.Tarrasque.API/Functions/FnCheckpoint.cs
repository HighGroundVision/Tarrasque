using DurableTask.Core;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.API.Entities;
using HGV.Tarrasque.API.Models;
using HGV.Tarrasque.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HGV.Tarrasque.API.Functions
{
    public class FnCheckpoint
    {
        private readonly IDotaService _service;

        public FnCheckpoint(IDotaService service)
        {
            _service = service;
        }
        
        [FunctionName("FnCheckpoint")]
        public async Task Checkpoint(
            [BlobTrigger("hgv-checkpoint/master.json")]Checkpoint checkpoint,
            [Blob("hgv-checkpoint/master.json")]TextWriter writer,
            [Queue("hgv-ad-matches")]IAsyncCollector<Match> queue,
            [DurableClient] IDurableEntityClient fnClient,
            ILogger log)
        {
            try
            {
                var collection = await _service.GetMatches(checkpoint, log);

                var modes = collection.GroupBy(_ => _.game_mode).Select(_ => new { Mode = _.Key, Matches = _.Count() }).ToList();
                foreach (var item in modes)
                {
                    var entityId = new EntityId(nameof(ModeCounter), item.Mode.ToString());
                    await fnClient.SignalEntityAsync<IModeCounter>(entityId, proxy => proxy.Add(item.Matches));
                }

                foreach (var item in collection)
                {
                    if (item.game_mode == 18)
                        await queue.AddAsync(item);
                }

                var end_time = collection.Select(_ => _.start_time + _.duration).Max();
                checkpoint.Delta = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(end_time);
                checkpoint.Latest = collection.Max(_ => _.match_seq_num) + 1;
                checkpoint.Processed = collection.Count();

                var delay = (checkpoint.Processed < 100) ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(1);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to Process Checkpoint");
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            finally
            {
                var json = JsonConvert.SerializeObject(checkpoint);
                await writer.WriteAsync(json);
            }
        }
    }
}