using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class AbilitiesStats
    {
        public int Wins { get; set; }
        public float WinRate { get; set; }

        public int Picks { get; set; }
        public float PickRate { get; set; }

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Damage { get; set; }         // Damage To Heroes
        public int Destruction { get; set; }    // Damage To Structures
        public int Gold { get; set; }
    }
}
