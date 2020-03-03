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

        public List<History> History { get; set; } = new List<History>();
        public List<PlayerSummary> Combatants { get; set; } = new List<PlayerSummary>();
    }

    public class PlayerSummary
    {
        public ulong AccountId { get; set; }
        public ulong SteamId { get; set; }
        public string Persona { get; set; }
        public string Avatar { get; set; }
        public bool Friend { get; set; }
        public List<History> With { get; set; } = new List<History>();
        public List<History> Against { get; set; } = new List<History>();
    }

    public class History
    {
        public ulong MatchId { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool Victory { get; set; }
        public HeroSummary Hero { get; set; }
        public List<AbilitySummary> Abilities { get; set; } = new List<AbilitySummary>();
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
