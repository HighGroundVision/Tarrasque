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
        // Task<Match> FetchMatch(long match);

        // Task StoreMatch(Match match, TextWriter writer);

        // Task UpdateRegion(Match match, TextReader reader, TextWriter writer);

        // Task UpdateHeroes(Match match, TextReader reader, TextWriter writer);

        // Task UpdateAbilities(Match match, TextReader reader, TextWriter writer);

        Task<Match> ReadMatch(TextReader reader);

        Task QueueRegions(Match match, IAsyncCollector<RegionReference> queue);
        Task QueueHeroes(Match match, IAsyncCollector<HeroReference> queue);
        Task QueueAbilities(Match match, IAsyncCollector<AbilityReference> queue);

        Task QueueAccounts(Match match, IAsyncCollector<AccountReference> queue);
    }

    public class ProcessMatchService : IProcessMatchService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;
        private readonly List<Ability> skills;

        public ProcessMatchService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = MetaClient.Instance.Value;
            this.skills = this.metaClient.GetSkills();
        }

        public async Task<Match> ReadMatch(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var input = await reader.ReadToEndAsync();
            var match = JsonConvert.DeserializeObject<Match>(input);
            return match;
        }

        /*
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

        */

        public async Task QueueRegions(Match match, IAsyncCollector<RegionReference> queue)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            await queue.AddAsync(new RegionReference()
            {
                Match = match.match_id,
                Region = match.GetRegion()
            });
        }

        public async Task QueueHeroes(Match match, IAsyncCollector<HeroReference> queue)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            var maxAssists = match.players.Max(_ => _.assists);
            var maxGold = match.players.Max(_ => _.gold);
            var maxKills = match.players.Max(_ => _.kills);
            var minDeaths = match.players.Min(_ => _.deaths);

            foreach (var player in match.players)
            {
                var item = new HeroReference();
                item.Match = match.match_id;
                item.Date = match.GetStart().ToString("yy-MM-dd");
                item.Region = match.GetRegion();
                item.Hero = player.hero_id;

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

                await queue.AddAsync(item);
            }
        }

        public async Task QueueAbilities(Match match, IAsyncCollector<AbilityReference> queue)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            var maxAssists = match.players.Max(_ => _.assists);
            var maxGold = match.players.Max(_ => _.gold);
            var maxKills = match.players.Max(_ => _.kills);
            var minDeaths = match.players.Min(_ => _.deaths);

            foreach (var player in match.players)
            {
                var abilities = player.ability_upgrades
                    .Select(_ => _.ability)
                    .Distinct()
                    .Join(this.skills, _ => _, _ => _.Id, (lhs, rhs) => rhs)
                    .ToList();

                foreach (var ability in abilities)
                {
                    var item = new AbilityReference();
                    item.Match = match.match_id;
                    item.Date = match.GetStart().ToString("yy-MM-dd");
                    item.Region = match.GetRegion();

                    item.DraftOrder = player.DraftOrder();

                    if (match.Victory(player))
                        item.Wins++;
                    else
                        item.Losses++;

                    if (ability.HeroId == player.hero_id)
                        item.HeroAbility++;

                    if (player.assists == maxAssists)
                        item.MaxAssists++;

                    if (player.gold == maxGold)
                        item.MaxGold++;

                    if (player.kills == maxKills)
                        item.MaxKills++;

                    if (player.deaths == minDeaths)
                        item.MinDeaths++;

                    await queue.AddAsync(item);
                }
            }

        }

        private const long CATCH_ALL_ACCOUNT = 4294967295;
        public async Task QueueAccounts(Match match, IAsyncCollector<AccountReference> queue)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            var players = match.players.Where(_ => _.account_id != CATCH_ALL_ACCOUNT).ToList();
            foreach (var player in players)
            {
                var abilities = player.ability_upgrades
                    .Select(_ => _.ability)
                    .Distinct()
                    .Join(skills, _ => _, _ => _.Id, (lhs, rhs) => rhs)
                    .ToList();

                var item = new AccountReference();
                item.Match = match.match_id;
                item.Account = player.account_id;
                // item.Victory = match.Victory(player);
                // item.Hero = player.hero_id;
                // item.Abilities = abilities;

                await queue.AddAsync(item);
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
