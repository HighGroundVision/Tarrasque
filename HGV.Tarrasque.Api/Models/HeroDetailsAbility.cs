using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Api.Models
{
    public class HeroDetailsAbility
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get { return this.Wins / (float)this.Total; } }
    }
}
