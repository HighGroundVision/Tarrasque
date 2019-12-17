using System;
using System.Collections.Generic;
using System.Text;

namespace Tarrasque.Collection.Models
{
    public class Checkpoint
    {
        public List<long> History { get; set; }
        public int Counter { get; set; }
    }
}
