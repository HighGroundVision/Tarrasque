using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessaAbilities.Models
{
    public class AbilitySummaryVictory
    {
        public DateTimeOffset Timestamp { get; set; }
        public bool Victory { get; set; }
        public bool Ancestry { get; set; }
        public int Priority { get; set; }
    }

    public class AbilitySummaryStat
    {
        public int Picks { get; set; }
        public int Wins { get; set; }
        public int Ancestry { get; set; }
        public int Priority { get; set; }
    }

    public class AbilitySummaryDelta
    {
        public AbilitySummaryStat Total { get; set; } = new AbilitySummaryStat();
        public AbilitySummaryStat Current { get; set; } = new AbilitySummaryStat();
        public AbilitySummaryStat Previous { get; set; } = new AbilitySummaryStat();
    }

    public class AbilitySummary : AbilitySummaryDelta
    {
        public List<AbilitySummaryVictory> Data { get; set; } = new List<AbilitySummaryVictory>();
    }
}
