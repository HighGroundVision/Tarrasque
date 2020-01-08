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
        Task ProcessAcount(AccountReference accountRef, TextReader reader, TextWriter writer);
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

        public async Task ProcessAcount(AccountReference accountRef,  TextReader reader, TextWriter writer)
        {
            Guard.Argument(accountRef, nameof(accountRef)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var profile = await GetProfile(accountRef.Steam);

            if (reader == null)
                await NewAccount(accountRef, profile, writer);
            else
                await UpdateAccount(accountRef, profile, reader, writer);
        }

        private async Task<Profile> GetProfile(ulong steamId)
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

            var player = await policy.ExecuteAsync<Profile>(async () =>
            {
                return await this.apiClient.GetPlayerSummary(steamId);
            });

            return player;
        }

        private static async Task NewAccount(AccountReference regionRef, Profile profile, TextWriter writer)
        {
            Guard.Argument(regionRef, nameof(regionRef)).NotNull();
            Guard.Argument(profile, nameof(profile)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var data = new AccountData();
            data.AccountId = regionRef.Account;
            data.SteamId = profile.steamid;

            SetAccountData(regionRef, profile, data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static async Task UpdateAccount(AccountReference regionRef, Profile profile, TextReader reader, TextWriter writer)
        {
            Guard.Argument(regionRef, nameof(regionRef)).NotNull();
            Guard.Argument(profile, nameof(profile)).NotNull();
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var input = await reader.ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<AccountData>(input);

            SetAccountData(regionRef, profile, data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static void SetAccountData(AccountReference regionRef, Profile profile, AccountData data)
        {
            data.Persona = profile.personaname;
            data.Avatar = profile.avatar;
            data.Matches.Add(regionRef.Match);
            data.Total++;

            if (regionRef.Victory)
            {
                data.Wins++;
                data.AddHeroWin(regionRef.Hero);
                data.AddAbilitiesWin(regionRef.Abilities);
            }
            else
            {
                data.Losses++;
                data.AddHeroLose(regionRef.Hero);
                data.AddAbilitiesLose(regionRef.Abilities);
            }
        }

    }
}
