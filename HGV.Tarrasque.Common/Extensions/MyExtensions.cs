using System;
using System.Linq;
using System.Collections.Generic;
using HGV.Basilius;

namespace HGV.Tarrasque.Common.Extensions
{
    public static class MyExtensions
    {

        public static int GetRegion(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return MetaClient.Instance.Value.GetRegionId(match.cluster);
        }

        public static string GetDate(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return GetStart(match).ToString("yy-MM-dd");
        }

        public static DateTimeOffset GetStart(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return DateTimeOffset.FromUnixTimeSeconds(match.start_time);
        }

        public static DateTimeOffset GetEnd(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return DateTimeOffset.FromUnixTimeSeconds(match.start_time).Add(match.GetDuration());
        }

        public static TimeSpan GetDuration(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return DateTimeOffset.FromUnixTimeSeconds(match.duration).TimeOfDay;
        }

        public static DateTimeOffset GetStart(this HGV.Daedalus.GetMatchHistory.Match match)
        {
            return DateTimeOffset.FromUnixTimeSeconds(match.start_time);
        }

        public static bool Victory(this HGV.Daedalus.GetMatchDetails.Match match, HGV.Daedalus.GetMatchDetails.Player player)
        {
            return (match.radiant_win && player.player_slot < 6);
        }

        public static int Region(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return HGV.Basilius.MetaClient.Instance.Value.GetRegionId(match.cluster);
        }

        public static int PickPriority(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            switch (player.player_slot)
            {
                case 0: return 0;
                case 128: return 1;
                case 1: return 2;
                case 129: return 3;
                case 2: return 4;
                case 130: return 5;
                case 3: return 6;
                case 131: return 7;
                case 4: return 8;
                case 132: return 9;
                default: return 0;
            }
        }

        public static ulong SteamId(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            return (ulong)player.account_id + 76561197960265728L;
        }

        public static Hero GetHero(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            var heroes = MetaClient.Instance.Value.GetHeroes();
            return heroes.Find(_ => _.Id == player.hero_id);
        }

        public static IList<Ability> GetSkills(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            var skills = MetaClient.Instance.Value.GetSkills();
            if (player.ability_upgrades == null)
                return new List<Ability>();
            else
                return player.ability_upgrades.Select(_ => _.ability).Distinct().Join(skills, _ => _, _ => _.Id, (lhs, rhs) => rhs).ToList();
        }

        public static IList<Talent> GetTalenets(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            var talents = MetaClient.Instance.Value.GetTalents();
            if (player.ability_upgrades == null)
                return new List<Talent>();
            else
                return player.ability_upgrades.Select(_ => _.ability).Distinct().Join(talents, _ => _, _ => _.Id, (lhs, rhs) => rhs).ToList();
        }

        public static IList<Ability> GetAbilities(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            var abilities = MetaClient.Instance.Value.GetAbilities();
            if (player.ability_upgrades == null)
                return new List<Ability>();
            else
                return player.ability_upgrades.Select(_ => _.ability).Distinct().Join(abilities, _ => _, _ => _.Id, (lhs, rhs) => rhs).ToList();
        }

        public static IList<Ability> GetUltimates(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            var ultimates = MetaClient.Instance.Value.GetUltimates();
            if (player.ability_upgrades == null)
                return new List<Ability>();
            else
                return player.ability_upgrades.Select(_ => _.ability).Distinct().Join(ultimates, _ => _, _ => _.Id, (lhs, rhs) => rhs).ToList();
        }

        public static IEnumerable<Tuple<Ability, Ability>> GetPairs(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            var skills = player.GetSkills();
            return from l1 in skills
                   from l2 in skills.Except(new[] { l1 })
                   where l1.Id < l2.Id
                   select Tuple.Create(l1, l2);
        }
    }
}
