using HGV.Daedalus.GetMatchDetails;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class MatchTimestamp
    {
        public ulong Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool Victory { get; set; }
    }

    public class HeroTimestamp
    {
        public int HeroId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool Victory { get; set; }
    }

    public class AbilityTimestamp
    {
        public int AbilityId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool Victory { get; set; }
    }

    public class HeroSummary
    {
        public int HeroId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Total { get; set; }
    }

    public class AbilitySummary
    {
        public int AbilityId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Total { get; set; }
    }

    public class AccountData
    {
        public uint AccountId { get; set; }
        public ulong SteamId { get; set; }
        public string Persona { get; set; }
        public string Avatar { get; set; }

        public IList<MatchTimestamp> Timeline { get; set; }
        public IList<HeroTimestamp> HeroTimestamps { get; set; }
        public IList<AbilityTimestamp> AbilityTimestamps { get; set; }
        public IList<HeroSummary> HeroSummarys { get; set; }
        public IList<AbilitySummary> AbilitySummarys { get; set; }

        public AccountData()
        {
            this.Timeline = new List<MatchTimestamp>();
            this.HeroTimestamps = new List<HeroTimestamp>();
            this.AbilityTimestamps = new List<AbilityTimestamp>();
            this.HeroSummarys = new List<HeroSummary>();
            this.AbilitySummarys = new List<AbilitySummary>();
        }
    }
}
