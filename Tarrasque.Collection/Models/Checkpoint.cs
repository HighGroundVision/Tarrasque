using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{
    public class Checkpoint
    {
        public DateTime Timestamp { get; set; }
        public long Latest { get; set; }
        public List<long> History { get; set; }
        public int Counter { get; set; }
    }
}
