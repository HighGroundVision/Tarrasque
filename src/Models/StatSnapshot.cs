using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public struct StatSnapshot
    {
        public int Type { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public bool Win { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Damage { get; set; }         // Damage To Heroes
        public int Destruction { get; set; }    // Damage To Structures
        public int Gold { get; set; }
    }
}
