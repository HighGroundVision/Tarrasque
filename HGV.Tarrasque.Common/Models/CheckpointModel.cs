using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class CheckpointModel
    {
        public int Batch { get; set; }
        public string Delta { get; set; }
        public ulong Latest { get; set; }
        public int Total { get; set; }
        public int InQueue { get; set; }
    }
}
