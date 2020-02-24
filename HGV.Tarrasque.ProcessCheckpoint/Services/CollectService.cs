using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessCheckpoint.Exceptions;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Services
{
    public interface ICollectService
    {
        Task ProcessCheckpoint(TextReader reader, TextWriter writer, CloudQueue queue, ILogger log);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient apiClient;

        public CollectService(IDotaApiClient client)
        {
            this.apiClient = client;
        }

        public async Task ProcessCheckpoint(TextReader reader, TextWriter writer, CloudQueue queue, ILogger log)
        {
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();
            Guard.Argument(queue, nameof(queue)).NotNull();
            Guard.Argument(log, nameof(log)).NotNull();

            var checkpoint = await GetCheckpoint(reader);

            try
            {
                var matches = await GetMatches(checkpoint.Latest, log);
                var collection = matches.Where(_ => _.game_mode == 18).ToList();
                var max = matches.Select(_ => _.start_time + _.duration).Max();

                // Update Checkpoint
                checkpoint.Total += collection.Count;
                checkpoint.Batch = matches.Count();
                checkpoint.Delta = (DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(max)).Humanize(2);
                checkpoint.Latest = matches.Max(_ => _.match_seq_num) + 1;
                checkpoint.InQueue = queue.ApproximateMessageCount.GetValueOrDefault();

                foreach (var item in collection)
                {
                    var json = JsonConvert.SerializeObject(item);
                    var msg = new CloudQueueMessage(json);
                    await queue.AddMessageAsync(msg);
                }
            }
            catch (BelowLimitException)
            {
                log.LogError("Below Limit");
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                log.LogError("To Many Requests");
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("500"))
            {
                log.LogError("Service Error");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(1));
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

        private async Task NextCheckpoint(CheckpointModel checkpoint, TextWriter writer)
        {
            Guard.Argument(checkpoint, nameof(checkpoint)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var json = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(json);
        }

        private async Task<List<Match>> GetMatches(ulong latest, ILogger log)
        {
            Guard.Argument(latest, nameof(latest)).NotDefault();
            Guard.Argument(log, nameof(log)).NotNull();

            var matches = await this.apiClient.GetMatchesInSequence(latest);
            if (matches.Count < 100)
                throw new BelowLimitException();

            return matches;
        }
    }
}
