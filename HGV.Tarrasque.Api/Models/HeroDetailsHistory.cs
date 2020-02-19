using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Api.Models
{
    public class HeroDetailsHistory
    {
        public string Date { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public float WinRate { get; set; }
    }
}
