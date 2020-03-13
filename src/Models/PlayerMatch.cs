using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class PlayerMatch
    {
        // When
        public string Id { get; set; }
        public long MatchId { get; set; }
        public long SequenceId { get; set; }
        public DateTimeOffset Date { get; set; }
        public TimeSpan Duration { get; set; }

        // Where
        public int Cluster { get; set; }
        public int Region { get; set; }

        // Whom
        public long AccountId { get; set; }
        public bool Anonymous { get; set; }
        public int Slot { get; set; }
        public int Team { get; set; }
        public int Status { get; set; }

        // What
        public int HeroId { get; set; }
        public List<int> Abilities { get; set; } = new List<int>();
        public List<int> Ultimates { get; set; } = new List<int>();
        public List<int> Talents { get; set; } = new List<int>();
        public List<int> Items { get; set; } = new List<int>();
        public int NeutralItem { get; set; }

        // Stats
        public int Victory { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int LastHists { get; set; }
        public int Denies { get; set; }
        public int Level { get; set; }
        public int Gold { get; set; }
        public int GoldSpent { get; set; }
        public int GPM { get; set; }
        public int XPM { get; set; }
        public int HeroDamage { get; set; }
        public int TowerDamage { get; set; }
        public int HeroHealing { get; set; }
    }
}