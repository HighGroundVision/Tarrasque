using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessRegions.DTO
{
    public class Region
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Total { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
