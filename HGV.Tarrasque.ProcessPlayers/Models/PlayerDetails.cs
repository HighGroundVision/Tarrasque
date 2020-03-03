using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessPlayers.Models
{
    public class PlayerDetail
    {
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
        public ulong SteamId { get { return AccountId + 76561197960265728L; } }
        public bool Friend { get; set; }
        public List<History> With { get; set; } = new List<History>();
        public List<History> Against { get; set; } = new List<History>();
    }
}
