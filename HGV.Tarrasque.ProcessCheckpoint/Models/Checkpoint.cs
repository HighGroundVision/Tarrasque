using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessCheckpoint.Models
{
    public class Checkpoint
    {
        public ulong Latest { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public int Total { get; set; }
        public int ADTotal { get; set; }
    }
}
