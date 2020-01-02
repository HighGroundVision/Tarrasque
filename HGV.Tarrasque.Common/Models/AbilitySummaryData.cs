using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class AbilityData
    {
        public string AbilityId { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int DraftOrder { get; set; }
        public int HeroAbility { get; set; }
        public int MaxKills { get; set; }
        public int MaxAssists { get; set; }
        public int MinDeaths { get; set; }
        public int MaxGold { get; set; }

        public AbilityData()
        {
            this.AbilityId = "0";
        }
    }

    public class AbilitySummaryData
    {
        public int Region { get; set; }
        public DateTime Date { get; set; }
        public List<AbilityData> Abilities { get; set; }

        public AbilitySummaryData(Match item)
        {
            this.Region = item.GetRegion();
            this.Date = item.GetStart().Date;
            this.Abilities = new List<AbilityData>();
        }

        public AbilitySummaryData()
        {

        }
    }
}