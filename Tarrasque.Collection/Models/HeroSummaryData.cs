using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{
    public class HeroData 
    {
        public int HeroId { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }

        public int DraftOrder { get; set; }
        public int MaxKills { get; set; }
        public int MaxAssists { get; set; }
        public int MinDeaths { get; set; }
        public int MaxGold { get; set; }
    }

    public class HeroSummaryData
    {
        public  int Region { get; set; }
        public DateTimeOffset Date { get; set; }
        public List<HeroData> Heroes { get; set; }

        public HeroSummaryData()
        {
            this.Heroes = new List<HeroData>();
        }
    }
}
