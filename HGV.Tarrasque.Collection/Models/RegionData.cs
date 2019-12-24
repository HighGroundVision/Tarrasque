using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{
    public class RegionData
    {
        public int Id { get; set; }

        public Dictionary<DateTime, int> Range { get; set; }

        public RegionData()
        {
            this.Range = new Dictionary<DateTime, int>();
        }
    }
}
