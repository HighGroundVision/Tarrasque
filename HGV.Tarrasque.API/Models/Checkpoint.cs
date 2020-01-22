
using System;
using System.Collections.Generic;

namespace HGV.Tarrasque.API.Models
{
    public class Checkpoint
    {
        public TimeSpan Delta { get; set; }
        public int Processed { get; set; }
        public ulong Latest { get; set; }
        public List<ulong> History { get; set; } = new List<ulong>();
    }
}
