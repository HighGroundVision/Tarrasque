using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.Services
{
    public interface IMatchConverterService
    {
        IEnumerable<PlayerMatch> Convert(Match match);
    }

    public class MatchConverterService : IMatchConverterService
    {
        private readonly MetaClient metaClient;
        private const ulong CATCH_ALL_ACCOUNT = 4294967295;
        private List<int> abilities;
        private List<int> ultimates;
        private List<int> talents;

        public MatchConverterService()
        {
            this.metaClient =  MetaClient.Instance.Value;
            this.abilities = this.metaClient.GetAbilities().Select(_ => _.Id).ToList();
            this.ultimates = this.metaClient.GetUltimates().Select(_ => _.Id).ToList();
            this.talents = this.metaClient.GetTalents().Select(_ => _.Id).ToList();
        }

        public IEnumerable<PlayerMatch> Convert(Match match)
        {
            var wins = 0;
            var loses = 0;

            foreach (var player in match.players)
            {
                var data = new PlayerMatch();
                data.Id = Guid.NewGuid().ToString();
                data.MatchId = (long)match.match_id;
                data.SequenceId = (long)match.match_seq_num;
                data.Date = DateTimeOffset.FromUnixTimeSeconds(match.start_time);
                data.Duration = DateTimeOffset.FromUnixTimeSeconds(match.duration).TimeOfDay;
                data.Cluster = match.cluster;
                data.Region = this.metaClient.GetRegionId(match.cluster);
                data.AccountId = player.account_id;
                data.Anonymous = player.account_id == CATCH_ALL_ACCOUNT;
                data.Slot = (player.player_slot < 5 ? player.player_slot : player.player_slot - 123) + 1;
                data.Team = player.player_slot < 5 ? 1 : 0;
                data.Victory = IsVictory(match, player);
                data.Status = player.leaver_status;
                data.HeroId = player.hero_id;
                data.Kills = player.kills;
                data.Deaths = player.deaths;
                data.Assists = player.assists;
                data.LastHists = player.last_hits;
                data.Denies = player.denies;
                data.Level = player.level;
                data.Gold = player.gold;
                data.GoldSpent = player.gold_spent;
                data.GPM = player.gold_per_min;
                data.XPM = player.xp_per_min;
                data.HeroDamage = player.hero_damage;
                data.TowerDamage = player.tower_damage;
                data.HeroHealing = player.hero_healing;

                if (player.ability_upgrades != null)
                {
                    var query = player.ability_upgrades.Select(_ => _.ability).Distinct().ToList();
                    data.Abilities = query.Where(_ => abilities.Contains(_)).ToList();
                    data.Ultimates = query.Where(_ => ultimates.Contains(_)).ToList();
                    data.Talents = query.Where(_ => talents.Contains(_)).ToList();
                }

                data.NeutralItem = player.item_neutral;

                data.Items.Add(player.item_0);
                data.Items.Add(player.item_1);
                data.Items.Add(player.item_2);
                data.Items.Add(player.item_3);
                data.Items.Add(player.item_4);
                data.Items.Add(player.item_5);
                data.Items.Add(player.backpack_0);
                data.Items.Add(player.backpack_1);
                data.Items.Add(player.backpack_2);
                data.Items.Remove(0);

                if (data.Victory == 1)
                    wins++;
                else
                    loses++;

                yield return data;
            }

            if (wins != loses)
                throw new Exception();
        }

        private static int IsVictory(Match match, Player player)
        {
            if (match.radiant_win)
                return player.player_slot < 5 ? 1 : 0;
            else
                return player.player_slot > 5 ? 1 : 0;
        }
    }
}
