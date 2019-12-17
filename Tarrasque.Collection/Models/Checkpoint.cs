using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{
    public class Checkpoint
    {
        public List<long> History { get; set; }
        public int Counter { get; set; }
    }
}
