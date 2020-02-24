using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessCheckpoint.Models
{
    public class CheckpointStatus
    {
        public int TotalAllMatches { get; set; }
        public int TotalADMatches { get; set; }
        public string Delta { get; set; }
        public Dictionary<string, int> Queues { get; set; } = new Dictionary<string, int>();
    }
}
