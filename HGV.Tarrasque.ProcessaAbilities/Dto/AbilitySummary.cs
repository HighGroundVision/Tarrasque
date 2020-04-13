using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessAbilities.DTO
{
    public class AbilitySummaryHistory
    {
        //public int Picks { get; set; }
        //public int Wins { get; set; }
        public float Ancestry { get; set; }
        public float Priority { get; set; }
        public float WinRate { get; set; }
    }

    public class AbilitySummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public AbilitySummaryHistory Total { get; set; } = new AbilitySummaryHistory();
        public AbilitySummaryHistory Current { get; set; } = new AbilitySummaryHistory();
        public AbilitySummaryHistory Previous { get; set; } = new AbilitySummaryHistory();
    }
}
