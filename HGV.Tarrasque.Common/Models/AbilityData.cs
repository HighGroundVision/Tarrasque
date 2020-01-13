using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class AbilityData
    {
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int DraftOrder { get; set; }
        public int HeroAbility { get; set; }
        public int MaxKills { get; set; }
        public int MaxAssists { get; set; }
        public int MinDeaths { get; set; }
        public int MaxGold { get; set; }

        public static AbilityData operator +(AbilityData lhs, AbilityData rhs)
        {
            var data = new AbilityData();
            data.Total = lhs.Total + rhs.Total;
            data.Wins = lhs.Wins + rhs.Wins;
            data.Losses = lhs.Losses + rhs.Losses;
            data.DraftOrder = lhs.DraftOrder + rhs.DraftOrder;
            data.MaxAssists = lhs.MaxAssists + rhs.MaxAssists;
            data.MaxKills = lhs.MaxKills + rhs.MaxKills;
            data.MinDeaths = lhs.MinDeaths + rhs.MinDeaths;
            data.HeroAbility = lhs.HeroAbility + rhs.HeroAbility;
            return data;
        }
    }
}