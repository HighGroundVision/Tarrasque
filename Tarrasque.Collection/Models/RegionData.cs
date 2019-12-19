using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{
    public class RegionData
    {
        public int Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public int TotalMatches { get; set; }
    }
}
