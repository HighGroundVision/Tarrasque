using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class AbilitySummary
    {
        public int Wins { get; set; }
        public int Picks { get; set; }

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Damage { get; set; }         // Damage To Heroes
        public int Destruction { get; set; }    // Damage To Structures
        public int Gold { get; set; }

        public double PickRate { get; set; }
        public double WinsRate { get; set; }
    }

    public class StatsDetail
    {
        public List<int> Abilities { get; set; }
        public AbilitySummary Melee { get; set; }
        public AbilitySummary Range { get; set; }
    }
}
