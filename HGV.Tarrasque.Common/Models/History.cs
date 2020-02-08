using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class History
    {
        public int TotalMatches { get; set; }
        public IEnumerable<ulong> Matches { get; private set; }

        public History()
        {
            this.Matches = new List<ulong>();
        }

        public void AddHistory(ulong item)
        {
            var temp = new List<ulong> { item };
            this.Matches = this.Matches.Concat(temp);
        }

    }
}
