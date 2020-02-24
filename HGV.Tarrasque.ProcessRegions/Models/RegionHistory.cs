using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessRegions.Models
{
    public class RegionHistory
    {
        public List<DateTimeOffset> Data { get; set; } = new List<DateTimeOffset>();
    }
}
