using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{

    public class Checkpoint
    {
        public IEnumerable<long> History { get; private set; }
        public TimeSpan Split { get; set; }
        public long Latest { get; set; }
        public int TotalMatches { get; set; }
        public int TotalADMatches { get; set; }

        public Checkpoint()
        {
            this.History = new List<long>();
        }

        public void AddHistory(long item)
        {
            var temp = new List<long> { item };
            this.History = this.History.Concat(temp);
        }
    }
}
