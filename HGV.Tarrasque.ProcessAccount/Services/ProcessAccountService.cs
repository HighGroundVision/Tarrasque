using Dawn;
using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using Polly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Profile = HGV.Daedalus.GetPlayerSummaries.Player;

namespace HGV.Tarrasque.ProcessAccount.Services
{
    public interface IProcessAccountService
    {
        Task<Match> ReadMatch(TextReader reader);
        Task<Profile> GetProfile(long steamId);
        Task UpdateAccount(long accountId, Match match, Profile profile, TextReader reader, TextWriter writer);
    }

    public class ProcessAccountService : IProcessAccountService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public ProcessAccountService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = MetaClient.Instance.Value;
        }

        public async Task<Profile> GetProfile(long steamId)
        {
            Guard.Argument(steamId, nameof(steamId)).Positive().NotZero();

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                });

            var player = await policy.ExecuteAsync<Daedalus.GetPlayerSummaries.Player>(async () =>
            {
                return await this.apiClient.GetPlayerSummary(steamId);
            });

            return player;
        }

        public async Task<Match> ReadMatch(TextReader reader)
        {
            Guard.Argument(reader, nameof(reader)).NotNull();

            var input = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<Match>(input);
        }

        public async Task UpdateAccount(long accountId, Match match, Profile profile, TextReader reader, TextWriter writer)
        {
            Guard.Argument(accountId, nameof(accountId)).NotNegative().NotZero();
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(profile, nameof(profile)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var player = GetPlayer(accountId, match);
            var skills = this.metaClient.GetSkills();
            var abilities = player.ability_upgrades
                .Select(_ => _.ability)
                .Distinct()
                .Join(skills, _ => _, _ => _.Id, (lhs, rhs) => lhs)
                .ToList();

            Func<AccountData> init = () =>
            {
                return new AccountData()
                {
                    AccountId = accountId,
                    SteamId = profile.steamid,
                    Avatar = profile.avatar,
                };
            };

            Action<AccountData> update = _ =>
            {
                _.Persona = profile.personaname;
                _.Avatar = profile.avatar;
                _.Matches.Add(match.match_id);
                _.Total++;

                if (match.Victory(player))
                {
                    _.Wins++;
                    _.AddHeroWin(player.hero_id);
                    _.AddAbilitiesWin(abilities);
                }
                else
                {
                    _.Losses++;
                    _.AddHeroLose(player.hero_id);
                    _.AddAbilitiesLose(abilities);
                }
            };

            await ReadUpdateWriteHandler(reader, writer, init, update);
        }

        private static Player GetPlayer(long AccountId, Match match)
        {
            var player = match.players.Find(_ => _.account_id == AccountId);
            if (player == null)
                throw new NullReferenceException($"Player [{AccountId}] is not part of Match [{match.match_id}]");

            return player;
        }

        private static async Task ReadUpdateWriteHandler<T>(TextReader reader, TextWriter writer, Func<T> init, Action<T> update) where T : class
        {
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
