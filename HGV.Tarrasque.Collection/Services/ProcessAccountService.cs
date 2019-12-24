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
using Polly.Wrap;

namespace HGV.Tarrasque.Collection.Services
{
    public interface IProcessAccountService
    {
        Task UpdateAccount(long AccountId, TextReader readerMatch, TextReader readerAccount, TextWriter writerAccount);
    }

    public class ProcessAccountService : IProcessAccountService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public ProcessAccountService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = new MetaClient();
        }

        public async Task UpdateAccount(long AccountId, TextReader readerMatch, TextReader readerAccount, TextWriter writerAccount)
        {
            var match = await ReadMatch(readerMatch);
            var account = await FetchAccount(AccountId);
            var player = GetPlayer(AccountId, match);
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
                    AccountId = AccountId,
                    SteamId = account.steamid,
                };
            };

            Action<AccountData> update = _ =>
            {
                _.Persona = account.personaname;
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

            await ReadUpdateWriteHandler(readerAccount, writerAccount, init, update);
        }

        private static Player GetPlayer(long AccountId, Match match)
        {
            var player = match.players.Find(_ => _.account_id == AccountId);
            if (player == null)
                throw new NullReferenceException($"Player [{AccountId}] is not part of Match [{match.match_id}]");

            return player;
        }

        private static async Task<Match> ReadMatch(TextReader reader)
        {
            var input = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<Match>(input);
        }


        private static async Task ReadUpdateWriteHandler<T>(TextReader reader, TextWriter writer, Func<T> init, Action<T> update) where T : class
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

        private  async Task<Daedalus.GetPlayerSummaries.Player> FetchAccount(long id)
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                });

            var player = await policy.ExecuteAsync<Daedalus.GetPlayerSummaries.Player>(async () =>
            {
                long steamID = ConvertDotaIdToSteamId(id);
                return await this.apiClient.GetPlayerSummary(steamID);
            });

            return player;
        }

        const long ID_OFFSET = 76561197960265728L;
        private static long ConvertDotaIdToSteamId(long input)
        {
            if (input < 1L)
                return 0;
            else
                return input + ID_OFFSET;
        }
    }
}
