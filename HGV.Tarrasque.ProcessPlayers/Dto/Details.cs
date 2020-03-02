using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessPlayers.DTO
{
    public class Details
    {
        public int Total { get; set; }
        public float WinRate { get; set; }
        public List<History> History { get; set; } = new List<History>();
        public List<PlayerSummary> Combatants { get; set; } = new List<PlayerSummary>();
    }

    public class PlayerSummary
    {
        public bool Friend { get; set; }
        public List<History> With { get; set; } = new List<History>();
        public List<History> Against { get; set; } = new List<History>();
    }

    public class History
    {
        public ulong MatchId { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool Victory { get; set; }
        public EntitySummary Hero { get; set; }
        public List<EntitySummary> Abilities { get; set; } = new List<EntitySummary>();
    }

    public class EntitySummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
    }
}
