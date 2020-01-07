using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class RegionReference
    {
        public long Match { get; set; }

        public int Region { get; set; }

        public DateTime Date { get; set; }
    }
}
