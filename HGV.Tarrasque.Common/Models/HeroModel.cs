using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class HeroModel
    {
        public int HeroId { get; set; }
        public string HeroName { get; set; }
        public string Date { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get { return Total - Wins; } }
        public float WinRate { get { return (float)Wins / (float)Total;  } }
    }
}
