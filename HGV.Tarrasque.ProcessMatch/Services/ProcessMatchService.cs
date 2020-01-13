using Dawn;
using HGV.Basilius;
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

namespace HGV.Tarrasque.ProcessMatch.Services
{
    public interface IProcessMatchService
    {
        Task<Match> FetchMatch(ulong match);

        Task ProcessMatch(Match match, IBinder binder);
    }

    public class ProcessMatchService : IProcessMatchService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public ProcessMatchService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = MetaClient.Instance.Value;
        }

        public async Task<Match> FetchMatch(ulong matchId)
        {
            Guard.Argument(matchId, nameof(matchId)).Positive().NotZero();

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                });

            var match = await policy.ExecuteAsync<Match>(async () =>
            {
                var details = await this.apiClient.GetMatchDetails(matchId);
                return details;
            });

            return match;
        }

        public async Task ProcessMatch(Match match, IBinder binder)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(binder, nameof(binder)).NotNull();
            
            await ProcessRegion(match, binder);
            await ProcessHero(match, binder);
        }

        #region Region

        private async Task ProcessRegion(Match match, IBinder binder)
        {
            var regionId = match.GetRegion();
            var date = match.GetStart().ToString("yy-MM-dd");
            var reader = await binder.BindAsync<TextReader>(new BlobAttribute($"hgv-regions/{date}/summary.json"));

            var collection = new List<RegionData>();
            if (reader != null)
            {
                var input = await reader.ReadToEndAsync();
                collection = JsonConvert.DeserializeObject<List<RegionData>>(input);
            }

            var item = collection.Find(_ => _.Region == regionId);
            if (item == null)
            {
                collection.Add(new RegionData() { Region = regionId, Matches = 1 });
            }
            else
            {
                item.Matches++;
            }

            var writer = await binder.BindAsync<TextWriter>(new BlobAttribute($"hgv-regions/{date}/summary.json"));
            var output = JsonConvert.SerializeObject(collection);
            await writer.WriteAsync(output);
        }

        #endregion

        #region Hero

        public async Task ProcessHero(Match match, IBinder binder)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(binder, nameof(binder)).NotNull();

            var date = match.GetStart().ToString("yy-MM-dd");

            var maxAssists = match.players.Max(_ => _.assists);
            var maxGold = match.players.Max(_ => _.gold);
            var maxKills = match.players.Max(_ => _.kills);
            var minDeaths = match.players.Min(_ => _.deaths);

            foreach (var player in match.players)
            {
                var item = new HeroData();
                item.Total = 1;
                item.DraftOrder = player.DraftOrder();

                if (match.Victory(player))
                    item.Wins++;
                else
                    item.Losses++;

                if (player.assists == maxAssists)
                    item.MaxAssists++;

                if (player.gold == maxGold)
                    item.MaxGold++;

                if (player.kills == maxKills)
                    item.MaxKills++;

                if (player.deaths == minDeaths)
                    item.MinDeaths++;

                var reader = await binder.BindAsync<TextReader>(new BlobAttribute($"hgv-heroes/{date}/{player.hero_id}/data.json"));
                if (reader != null)
                {
                    var input = await reader.ReadToEndAsync();
                    var data = JsonConvert.DeserializeObject<HeroData>(input);
                    item += data;
                }

                var output = JsonConvert.SerializeObject(item);
                var writer = await binder.BindAsync<TextWriter>(new BlobAttribute($"hgv-heroes/{date}/{player.hero_id}/data.json"));
                await writer.WriteAsync(output);
            }
        }

        #endregion
    }
}
