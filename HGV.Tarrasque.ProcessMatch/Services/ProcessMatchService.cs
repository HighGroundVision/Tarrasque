using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Polly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Services
{
    public interface IProcessMatchService
    {
        Task<Match> FetchMatch(long match);

        Task StoreMatch(Match match, TextWriter writer);

        Task UpdateRegion(Match match, TextReader reader, TextWriter writer);

        Task UpdateHeroes(Match match, TextReader reader, TextWriter writer);

        Task UpdateAbilities(Match match, TextReader reader, TextWriter writer);

        Task QueueAccounts(Match match, IAsyncCollector<AccountReference> queue);
        
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

        public async Task<Match> FetchMatch(long id)
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

            var match = await policy.ExecuteAsync<Match>(async () =>
            {
                var details = await this.apiClient.GetMatchDetails(id);
                return details;
            });

            return match;
        }

        public async Task StoreMatch(Match match, TextWriter writer)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var json = JsonConvert.SerializeObject(match);
            await writer.WriteAsync(json);
        }


        public async Task UpdateRegion(Match match, TextReader reader, TextWriter writer)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            Func<RegionData> init = () =>
            {
                return new RegionData(match);
            };

            Action<RegionData> update = _ =>
            {
                var date = match.GetStart().Date;
                if (_.Range.ContainsKey(date))
                    _.Range[date]++;
                else
                    _.Range.Add(date, 1);
            };

            await this.ReadUpdateWriteHandler(reader, writer, init, update);
        }

        public async Task UpdateHeroes(Match match, TextReader reader, TextWriter writer)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            Func<HeroSummaryData> init = () =>
            {
                return new HeroSummaryData(match);
            };

            Action<HeroSummaryData> action = _ =>
            {
                var maxAssists = match.players.Max(_ => _.assists);
                var maxGold = match.players.Max(_ => _.gold);
                var maxKills = match.players.Max(_ => _.kills);
                var minDeaths = match.players.Min(_ => _.deaths);

                foreach (var player in match.players)
                {
                    var summary = _.Heroes.Where(h => h.HeroId == player.hero_id).FirstOrDefault();
                    if (summary == null)
                    {
                        summary = new HeroData() { HeroId = player.hero_id };
                        _.Heroes.Add(summary);
                    }

                    summary.DraftOrder += player.DraftOrder();
                    summary.Total++;

                    if(match.Victory(player))
                        summary.Wins++;
                    else
                        summary.Losses++;

                    if (player.assists == maxAssists)
                        summary.MaxAssists++;

                    if (player.gold == maxGold)
                        summary.MaxGold++;

                    if (player.kills == maxKills)
                        summary.MaxKills++;

                    if (player.deaths == minDeaths)
                        summary.MinDeaths++;
                }
            };

            await this.ReadUpdateWriteHandler(reader, writer, init, action);
        }

        public async Task UpdateAbilities(Match match, TextReader reader, TextWriter writer)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var skills = this.metaClient.GetSkills();

            Func<AbilitySummaryData> init = () =>
            {
                return new AbilitySummaryData(match);
            };

            Action<AbilitySummaryData> action = _ =>
            {
                var maxAssists = match.players.Max(_ => _.assists);
                var maxGold = match.players.Max(_ => _.gold);
                var maxKills = match.players.Max(_ => _.kills);
                var minDeaths = match.players.Min(_ => _.deaths);

                foreach (var player in match.players)
                {
                    var abilities = player.ability_upgrades
                        .Select(_ => _.ability)
                        .Distinct()
                        .Join(skills, _ => _, _ => _.Id, (lhs, rhs) => rhs)
                        .ToList();

                    foreach (var ability in abilities)
                    {
                        var summary = _.Abilities.Where(h => h.AbilityId == ability.Id).FirstOrDefault();
                        if (summary == null)
                        {
                            summary = new AbilityData() { AbilityId = ability.Id };
                            _.Abilities.Add(summary);
                        }

                        summary.Total++;
                        summary.DraftOrder += player.DraftOrder();

                        if (match.Victory(player))
                            summary.Wins++;
                        else
                            summary.Losses++;

                        if(ability.HeroId == player.hero_id)
                            summary.HeroAbility++;

                        if(player.assists == maxAssists)
                            summary.MaxAssists++;

                        if (player.gold == maxGold)
                            summary.MaxGold++;

                        if (player.kills == maxKills)
                            summary.MaxKills++;

                        if (player.deaths == minDeaths)
                            summary.MinDeaths++;
                    }
                }
            };

            await this.ReadUpdateWriteHandler(reader, writer, init, action);
        }

        private const long CATCH_ALL_ACCOUNT = 4294967295;
        public async Task QueueAccounts(Match match, IAsyncCollector<AccountReference> queue)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            var players = match.players.Where(_ => _.account_id != CATCH_ALL_ACCOUNT).ToList();
            foreach (var player in players)
            {
                await queue.AddAsync(new AccountReference() { 
                    Account = player.account_id, 
                    Match = match.match_id
                });
            }
        }

        private async Task ReadUpdateWriteHandler<T>(TextReader reader, TextWriter writer, Func<T> init, Action<T> update) where T : class
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (reader == null)
                reader = new StringReader(string.Empty);

            var input = await reader.ReadToEndAsync();
            var data = string.IsNullOrWhiteSpace(input) ? init() : JsonConvert.DeserializeObject<T>(input);

            update(data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        
    }
}
