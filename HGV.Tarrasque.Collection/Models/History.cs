using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{
    public class History
    {
        public int TotalMatches { get; set; }
        public IEnumerable<long> Matches { get; private set; }

        public History()
        {
            this.Matches = new List<long>();
        }

        public void AddHistory(long item)
        {
            var temp = new List<long> { item };
            this.Matches = this.Matches.Concat(temp);
        }

    }
}
