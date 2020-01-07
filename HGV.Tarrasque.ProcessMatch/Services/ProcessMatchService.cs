﻿using HGV.Basilius;
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

        
    }
}