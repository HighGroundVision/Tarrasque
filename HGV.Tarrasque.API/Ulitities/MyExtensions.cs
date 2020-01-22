using HGV.Daedalus.GetMatchDetails;
using HGV.Basilius;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.API
{
    public static class MyExtensions
    {
        public static List<int> GetAbilities(this Player player)
        {
            if (player.ability_upgrades == null)
                return new List<int>();

            var skills = MetaClient.Instance.Value.GetSkills();
            var collection = player.ability_upgrades.Select(_ => _.ability).Join(skills, _ => _, _ => _.Id, (lhs, rhs) => lhs).ToList();
            return collection;
        }

        public static List<int> GetTalents(this Player player)
        {
            if (player.ability_upgrades == null)
                return new List<int>();

            var talents = MetaClient.Instance.Value.GetTalents();
            var collection = player.ability_upgrades.Select(_ => _.ability).Join(talents, _ => _, _ => _.Id, (lhs, rhs) => lhs).ToList();
            return collection;
        }
    }
}
