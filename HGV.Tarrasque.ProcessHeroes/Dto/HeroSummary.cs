using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessHeroes.DTO
{
    public class HeroSummaryHistory
    {
        public int Picks { get; set; }
        public int Wins { get; set; }
        public float WinRate { get { return this.Wins / (float)this.Picks; } }
    }

    public class HeroSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageIcon { get; set; }
        public string ImageBanner { get; set; }
        public string ImageProfile { get; set; }
        public HeroSummaryHistory Total { get; set; } = new HeroSummaryHistory();
        public HeroSummaryHistory Current { get; set; } = new HeroSummaryHistory();
        public HeroSummaryHistory Previous { get; set; } = new HeroSummaryHistory();
    }
}
