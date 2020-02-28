using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessAbilities.Models
{
    public class AbilityHistoryVictory
    {
        public DateTimeOffset Timestamp { get; set; }
        public bool Victory { get; set; }
        public bool Ancestry { get; set; }
        public int Priority { get; set; }
    }

    public class AbilityHistoryStat
    {
        public int Picks { get; set; }
        public int Wins { get; set; }
        public int Ancestry { get; set; }
        public int Priority { get; set; }
    }

    public class AbilityHistoryData
    {
        public AbilityHistoryStat Total { get; set; } = new AbilityHistoryStat();
        public AbilityHistoryStat Current { get; set; } = new AbilityHistoryStat();
        public AbilityHistoryStat Previous { get; set; } = new AbilityHistoryStat();
    }

    public class AbilityHistory : AbilityHistoryData
    {
        public List<AbilityHistoryVictory> Data { get; set; } = new List<AbilityHistoryVictory>();
    }
}
