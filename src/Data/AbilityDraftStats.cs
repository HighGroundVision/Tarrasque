using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Data
{
    public class AbilityDraftStat
    {
        public List<int> Abilities { get; set; }

        public int Total { get; set; }
        public double WinRate { get; set; }
        public double PickRate { get; set; }

        public int Wins { get; set; }
        public int Picks { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assist { get; set; }
    }
}
