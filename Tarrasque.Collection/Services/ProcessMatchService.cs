using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Collection.Extensions;
using HGV.Tarrasque.Collection.Models;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace HGV.Tarrasque.Collection.Services
{
    public interface IProcessMatchService
    {
        Task FetchMatch(long id);

        Task StoreMatch(TextWriter writer);

        Task UpdateRegion(TextReader reader, TextWriter writer);

        Task UpdateHeroes(TextReader reader, TextWriter writer);

        Task UpdateAbilities(TextReader reader, TextWriter writer);

        Task QueueAccounts(IAsyncCollector<AccountReference> queue);
    }

    public class ProcessMatchService : IProcessMatchService
    {
        private readonly IDotaApiClient client;
        private readonly MetaClient metaClient;

        private Match match { get; set; }

        public ProcessMatchService(IDotaApiClient client)
        {
            this.client = client;
            this.metaClient = new MetaClient();
            this.match = null;

            // TOOD: Update Player Summary [Day, Region, AccountId]
            // - Matches: []
            // - Total: #
            // - Wins: #
            // - Loses: #
        }

        public async Task FetchMatch(long id)
        {
            var waitAndRetryPolicy = Policy
               .Handle<Exception>(e => !(e is BrokenCircuitException)) // NOTE: Exception filtering! 
               .WaitAndRetryForeverAsync(attempt => TimeSpan.FromMilliseconds(200));

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            var policy = Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicy);

            this.match = await policy.ExecuteAsync<Match>(async () =>
            {
                return await client.GetMatchDetails(id);
            });
        }

        public async Task StoreMatch(TextWriter writer)
        {
            if (this.match == null)
                throw new NullReferenceException("c2baf44bcb1d4349a7051355123cfe4d");

            var json = JsonConvert.SerializeObject(this.match);
            await writer.WriteAsync(json);
        }


        public async Task UpdateRegion(TextReader reader, TextWriter writer)
        {
            var start = new RegionData()
            {
                Id = this.metaClient.ConvertClusterToRegion(this.match.cluster),
                Date = this.match.GetStart().Date,
                TotalMatches = 0,
            };

            await this.ReadUpdateWriteHandler(reader, writer, start, _ =>
            {
                _.TotalMatches++;
            });

            /*
            if (this.match == null)
                throw new NullReferenceException("34131343c0f2437c813d96def8ae5142");

            var input = await reader.ReadToEndAsync();

            RegionData data = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                var region = this.metaClient.ConvertClusterToRegion(this.match.cluster);
                var day = this.match.GetStart().Date;

                data = new RegionData()
                {
                    Id = region,
                    Date = day,
                    TotalMatches = 0,
                };
            }
            else
            {
                data = JsonConvert.DeserializeObject<Models.RegionData>(input);
            }

            data.TotalMatches++;

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
            */
        }

        public async Task UpdateHeroes(TextReader reader, TextWriter writer)
        {
            var start = new HeroSummaryData()
            {
                Region = this.metaClient.ConvertClusterToRegion(this.match.cluster),
                Date = this.match.GetStart().Date,
            };

            Action<HeroSummaryData> action = _ =>
            {
                var maxAssists = this.match.players.Max(_ => _.assists);
                var maxGold = this.match.players.Max(_ => _.gold);
                var maxKills = this.match.players.Max(_ => _.kills);
                var minDeaths = this.match.players.Min(_ => _.deaths);

                foreach (var player in this.match.players)
                {
                    var summary = _.Heroes.Where(h => h.HeroId == player.hero_id).FirstOrDefault();
                    if (summary == null)
                    {
                        summary = new HeroData() { HeroId = player.hero_id };
                        _.Heroes.Add(summary);
                    }

                    summary.DraftOrder += player.DraftOrder();
                    summary.Total++;

                    if(this.match.Victory(player))
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

            await this.ReadUpdateWriteHandler(reader, writer, start, action);
        }

        public async Task UpdateAbilities(TextReader reader, TextWriter writer)
        {
            var skills = this.metaClient.GetSkills();

            var start = new AbilitySummaryData()
            {
                Region = this.metaClient.ConvertClusterToRegion(this.match.cluster),
                Date = this.match.GetStart().Date,
            };

            Action<AbilitySummaryData> action = _ =>
            {
                var maxAssists = this.match.players.Max(_ => _.assists);
                var maxGold = this.match.players.Max(_ => _.gold);
                var maxKills = this.match.players.Max(_ => _.kills);
                var minDeaths = this.match.players.Min(_ => _.deaths);

                foreach (var player in this.match.players)
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

                        if (this.match.Victory(player))
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

            await this.ReadUpdateWriteHandler(reader, writer, start, action);
        }

        private const long CATCH_ALL_ACCOUNT = 4294967295;
        public async Task QueueAccounts(IAsyncCollector<AccountReference> queue)
        {
            var players = this.match.players.Where(_ => _.account_id != CATCH_ALL_ACCOUNT).ToList();
            foreach (var player in players)
            {
                await queue.AddAsync(new AccountReference() { Account = player.account_id, Match = this.match.match_id });
            }
        }

        private async Task ReadUpdateWriteHandler<T>(TextReader reader, TextWriter writer, T @default, Action<T> update) where T : class
        {
            if (this.match == null)
                throw new NullReferenceException("1cdfb95942494f99b00697fd7c0d6c8c");

            var input = await reader.ReadToEndAsync();

            T data = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                data = @default;
            }
            else
            {
                data = JsonConvert.DeserializeObject<T>(input);
            }

            update(data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

    }
}
