using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessHeroes.Models
{
    public class HeroSummaryVictory
    {
        public DateTimeOffset Timestamp {get; set;}
        public bool Victory { get; set; }
    }

    public class HeroSummaryStat
    {
        public int Picks { get; set; }
        public int Wins { get; set; }
    }

    public class HeroSummaryDelta
    {
        public HeroSummaryStat Total { get; set; } = new HeroSummaryStat();
        public HeroSummaryStat Current { get; set; } = new HeroSummaryStat();
        public HeroSummaryStat Previous { get; set; } = new HeroSummaryStat();
    }

    public class HeroSummary : HeroSummaryDelta
    {
        public List<HeroSummaryVictory> Data { get; set; } = new List<HeroSummaryVictory>();
    }
}
