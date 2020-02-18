using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessMatch.Services
{
    public interface IProcessPlayersService
    {
        Task ProcessMatch(Match match, IBinder binder, ILogger log);
    }

    public class ProcessPlayersService : IProcessPlayersService
    {
        private const long CATCH_ALL_ACCOUNT = 4294967295;
        private readonly IDotaApiClient apiClient;

        public ProcessPlayersService(IDotaApiClient client)
        {
            this.apiClient = client;
        }

        public async Task ProcessMatch(Match match, IBinder binder, ILogger log)
        {
            foreach (var player in match.players)
            {
                if (player.account_id == CATCH_ALL_ACCOUNT)
                    continue;

                var attr = new BlobAttribute($"hgv-players/{player.account_id}.json");
                var model = await ReadPlayer(attr, binder, log);
                await UpdatePlayer(match, player, model, log);
                await WritePlayer(attr, binder, model, log);
            }
        }

        private async Task<PlayerModel> ReadPlayer(BlobAttribute attr, IBinder binder, ILogger log)
        {
            try
            {
                var reader = await binder.BindAsync<TextReader>(attr);
                var json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<PlayerModel>(json);
            }
            catch (Exception)
            {
                return new PlayerModel();
            }
        }

        private async Task UpdatePlayer(Match match, Player player, PlayerModel model, ILogger log)
        {
            try
            {
                model.AccountId = player.account_id;
                model.SteamId = player.SteamId();
                model.Persona = player.persona;
                model.Total++;

                model.History.Add(new History()
                {
                    MatchId = match.match_id,
                    Date = match.GetStart(),
                    Hero = player.hero_id,
                    Victory = match.Victory(player),
                    Abilities = player.GetAbilities().Select(_ => _.Id).ToList()
                });

                model.WinRate = (float)model.History.Count(_ => _.Victory) / (float)model.Total;
                model.Rating = 0; // Some Kind of ELO raking system

                var friends = await GetFriends(player, log);

                foreach (var p in match.players)
                {
                    if (p.account_id == CATCH_ALL_ACCOUNT)
                        continue;

                    if (p.account_id == player.account_id)
                        continue;

                    // If Exists
                    var combatant = model.Combatants.Find(_ => _.AccountId == p.account_id);
                    if(combatant == null)
                    {
                        combatant = new PlayerSummary() 
                        { 
                            AccountId = p.account_id, 
                            Persona = p.persona,
                            SteamId = p.SteamId(),
                        };
                        model.Combatants.Add(combatant);
                    }

                    combatant.Friend = friends.Any(_ => _ == p.SteamId());

                    var history = new History()
                    {
                        MatchId = match.match_id,
                        Date = match.GetStart(),
                        Hero = p.hero_id,
                        Victory = match.Victory(p),
                        Abilities = p.GetAbilities().Select(_ => _.Id).ToList()
                    };

                    if (p.GetTeam() == player.GetTeam())
                        combatant.With.Add(history);
                    else 
                        combatant.Against.Add(history);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private async Task<List<ulong>> GetFriends(Player player, ILogger log)
        {
            var policy = Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(3,
                   retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                   (ex, timeout) => log.LogDebug(ex.Message)
               );

            var steamId = player.SteamId();
            var result = await policy.ExecuteAndCaptureAsync(async () =>
            {
                var list = await this.apiClient.GetFriendsList(steamId);
                return list.Select(_ => _.SteamId).ToList();
            });

            return result.Result ?? new List<ulong>();
        }

        private async Task WritePlayer(BlobAttribute attr, IBinder binder, PlayerModel model, ILogger log)
        {
            try
            {
                var json = JsonConvert.SerializeObject(model);
                var writer = await binder.BindAsync<TextWriter>(attr);
                await writer.WriteAsync(json);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }
    }
}
