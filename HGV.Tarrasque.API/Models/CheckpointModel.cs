
using System;
using System.Collections.Generic;

namespace HGV.Tarrasque.API.Models
{
    public class CheckpointModel
    {
        public TimeSpan Delta { get; set; }
        public int Processed { get; set; }
        public ulong Latest { get; set; }
    }
}
