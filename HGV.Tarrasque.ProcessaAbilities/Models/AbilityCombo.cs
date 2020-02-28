using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessAbilities.Models
{
    public class AbilityComboStat
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

    public class AbilityCombo
    {
        public List<AbilityComboStat> Heroes { get; set; } = new List<AbilityComboStat>();
        public List<AbilityComboStat> Abilities { get; set; } = new List<AbilityComboStat>();
    }
}
