using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessHeroes.Models
{
    public class HeroHistoryVictory
    {
        public ulong MatchId { get; set; }
        public DateTimeOffset Timestamp {get; set;}
        public bool Victory { get; set; }
    }

    public class HeroHistoryStat
    {
        public int Picks { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }
    }

    public class HeroHistoryData
    {
        public HeroHistoryStat Total { get; set; } = new HeroHistoryStat();
        public HeroHistoryStat Current { get; set; } = new HeroHistoryStat();
        public HeroHistoryStat Previous { get; set; } = new HeroHistoryStat();
    }

    public class HeroHistory : HeroHistoryData
    {
        public List<HeroHistoryVictory> Data { get; set; } = new List<HeroHistoryVictory>();
    }
}
