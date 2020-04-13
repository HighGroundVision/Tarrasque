using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessPlayers.DTO
{
    public class Details
    {
        public int AccountId { get; set; }
        public ulong SteamId { get { return (ulong)AccountId + 76561197960265728L; } }
        public string Persona { get; set; }
        public string Avatar { get; set; }

        public List<Summary> Summaries { get; set; } = new List<Summary>();
        public List<History> History { get; set; } = new List<History>();
        public List<PlayerSummary> Combatants { get; set; } = new List<PlayerSummary>();
    }

    public class Summary
    {
        public int RegionId { get; set; }
        public string RegionName { get; set; }
        public string RegionGroup { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double Ranking { get; set; }
        public bool Calibrated { get; set; }
        public double DeltaRaking { get; set; }
        public int DeltaTotal { get; set; }
    }

    public class PlayerSummary
    {
        public ulong AccountId { get; set; }
        public ulong SteamId { get; set; }
        public string Persona { get; set; }
        public string Avatar { get; set; }
        public bool Friend { get; set; }
        
        public int Total { get; set; }
        public int With { get; set; }
        public int VictoriesWith { get; set; }
        public int Against { get; set; }
        public int VictoriesAgainst { get; set; }
    }

    public class PlayerHistory
    {

        public ulong AccountId { get; set; }
        public ulong SteamId { get; set; }
        public string Persona { get; set; }
        public string Avatar { get; set; }
        public bool Friend { get; set; }

        public ulong MatchId { get; set; }
        public bool Victory { get; set; }
        public HeroSummary Hero { get; set; }
        public List<AbilitySummary> Abilities { get; set; } = new List<AbilitySummary>();
    }

    public class History
    {
        public ulong MatchId { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool Victory { get; set; }
        public HeroSummary Hero { get; set; }
        public List<AbilitySummary> Abilities { get; set; } = new List<AbilitySummary>();

        public List<PlayerHistory> With { get; set; } = new List<PlayerHistory>();
        public List<PlayerHistory> Against { get; set; } = new List<PlayerHistory>();
    }

    public class HeroSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
    }

    public class AbilitySummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public bool IsUltimate { get; set; }
    }
}
