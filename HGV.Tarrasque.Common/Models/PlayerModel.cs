using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class PlayerModel
    {
        public ulong AccountId { get; set; }
        public ulong SteamId { get; set; }

        public int Rating { get; set; }
        public int Total { get; set; }
        public float WinRate { get; set; }
        public List<History> History { get; set; } = new List<History>();

        public List<PlayerSummary> Combatants { get; set; } = new List<PlayerSummary>();
    }

    public class History
    {
        public ulong MatchId { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool Victory { get; set; }
        public int Hero { get; set; }
        public List<int> Abilities { get; set; } = new List<int>();
    }

    public class PlayerSummary
    {
        public ulong AccountId { get; set; }
        public ulong SteamId { get; set; }
        public string Persona { get; set; }
        public bool Friend { get; set; }
        public List<History> With { get; set; } = new List<History>();
        public List<History> Against { get; set; } = new List<History>();
    }

}
