using Dawn;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Exceptions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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
        Task ProcessCheckpoint(TextReader reader, TextWriter writer, List<IAsyncCollector<Match>> queues, ILogger log);
    }

    public class CollectService : ICollectService
    {
        private readonly IDotaApiClient apiClient;

        public CollectService(IDotaApiClient client)
        {
            this.apiClient = client;
        }

        public async Task ProcessCheckpoint(TextReader reader, TextWriter writer, List<IAsyncCollector<Match>> queues, ILogger log)
        {
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();
            Guard.Argument(queues, nameof(queues)).NotNull().NotEmpty();
            Guard.Argument(log, nameof(log)).NotNull();

            var checkpoint = await GetCheckpoint(reader);

            try
            {
                // Get Matches
                var matches = await GetMatches(checkpoint.Latest);
                var collection = GetValidAbilityDraftMatches(matches);

                // Update Checkpoint
                checkpoint.Total += matches.Count;
                checkpoint.ADTotal += collection.Count;
                checkpoint.Timestamp = matches.Max(_ => DateTimeOffset.FromUnixTimeSeconds(_.start_time + _.duration));
                checkpoint.Latest = matches.Max(_ => _.match_seq_num) + 1;

                // Queue
                foreach (var item in collection)
                    foreach (var q in queues)
                        await q.AddAsync(item);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            catch (BelowLimitException)
            {
                log.LogError("Below Limit");
                await Task.Delay(TimeSpan.FromMinutes(5));
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
     
        private async Task<Checkpoint> GetCheckpoint(TextReader reader)
        {
            Guard.Argument(reader, nameof(reader)).NotNull();

            var json = await reader.ReadToEndAsync();
            var checkpoint = JsonConvert.DeserializeObject<Checkpoint>(json);
            return checkpoint;
        }

        private async Task NextCheckpoint(Checkpoint checkpoint, TextWriter writer)
        {
            Guard.Argument(checkpoint, nameof(checkpoint)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var json = JsonConvert.SerializeObject(checkpoint);
            await writer.WriteAsync(json);
        }

        private List<Match> GetValidAbilityDraftMatches(List<Match> matches)
        {
            // Guards:
            // Only Ad Matches
            // Only Matches that are longer then 10 minutes

            var collection = matches
                    .Where(_ => _.game_mode == 18)
                    .Where(_ => _.duration > 600)
                    .ToList();

            return collection;
        }

        private async Task<List<Match>> GetMatches(ulong latest)
        {
            Guard.Argument(latest, nameof(latest)).NotDefault();

            var matches = await this.apiClient.GetMatchesInSequence(latest);
            if (matches.Count < 100)
                throw new BelowLimitException();

            return matches;
        }
    }
}
