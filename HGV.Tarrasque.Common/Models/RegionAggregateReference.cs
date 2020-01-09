using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class RegionAggregateReference
    {
        public List<string> Range { get; set; }

        public RegionAggregateReference()
        {
            this.Range = new List<string>();
        }
    }
}
