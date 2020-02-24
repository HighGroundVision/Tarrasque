using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessHeroes.Models
{
    public class HeroComboStat
    {
        public int Id { get; set; }
        public int Picks { get; set; }
        public int Wins { get; set; }

        public void Update(bool victory)
        {
            this.Picks++;
            this.Wins += victory ? 1 : 0;
        }
    }

    public class HeroCombo
    {
        public List<HeroComboStat> Data { get; set; } = new List<HeroComboStat>();
    }
}
