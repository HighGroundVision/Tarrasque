using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class RegionData
    {
        public int Id { get; set; }

        public Dictionary<string, int> Range { get; set; }

        public RegionData()
        {
            this.Range = new Dictionary<string, int>();
        }
    }
}
