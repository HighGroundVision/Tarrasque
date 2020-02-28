using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessAbilities.DTO
{
    public class AbilityDetailCombo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int Picks { get; set; }
        public int Wins { get; set; }
        public float WinRate { get { return this.Wins / (float)this.Picks; } }
    }

    public class AbilityDetailHistory
    {
        public string Day { get; set; }
        public int Picks { get; set; }
        public int Wins { get; set; }
        public float WinRate { get { return this.Wins / (float)this.Picks; } }
    }

    public class AbilityDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int OrginalHeroId { get; set; }

        public int Picks { get; set; }
        public int Wins { get; set; }
        public float WinRate { get { return this.Wins / (float)this.Picks; } }

        public List<AbilityDetailHistory> History { get; set; } = new List<AbilityDetailHistory>();
        public List<AbilityDetailCombo> Abilities { get; set; } = new List<AbilityDetailCombo>();
        public List<AbilityDetailCombo> Heroes { get; set; } = new List<AbilityDetailCombo>();
    }
}
