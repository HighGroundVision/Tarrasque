using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class RegionModel
    {
        public int Region { get; set; }
        public string Date { get; set; }
        public int Total { get; set; }
        public RegionModel() {}

        public RegionModel(int region, string date)
        {
            this.Region = region;
            this.Date = date;
            this.Total = 0;
        }
    }
}
