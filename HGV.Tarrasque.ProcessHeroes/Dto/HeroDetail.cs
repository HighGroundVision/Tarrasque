using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessHeroes.DTO
{
    public class HeroTalent
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public int Picks { get; set; }
        public int Wins { get; set; }
        public float WinRate { get { return this.Wins / (float)this.Picks; } }
    }

    public class HeroDetailCombo
    { 
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int Picks { get; set; }
        public int Wins { get; set; }
        public float WinRate { get { return this.Wins / (float)this.Picks; } }
        public bool IsUltimate { get; set; }
    }

    public class HeroDetailHistory
    {
        public string Day { get; set; }
        public int Picks { get; set; }
        public int Wins { get; set; }
        public float WinRate { get { return this.Wins / (float)this.Picks; } }
    }

    public class HeroDetail
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int Picks { get; set; }
        public int Wins { get; set; }
        public float WinRate { get { return this.Wins / (float)this.Picks; } }
        public List<HeroDetailHistory> History { get; set; } = new List<HeroDetailHistory>();
        public List<HeroDetailAttribute> Attributes { get; set; } = new List<HeroDetailAttribute>();
        public Dictionary<int,List<HeroTalent>> Talents { get; set; } = new Dictionary<int, List<HeroTalent>>();
        public List<HeroDetailCombo> Abilities { get; set; } = new List<HeroDetailCombo>();
        public List<HeroDetailCombo> Combos { get; set; } = new List<HeroDetailCombo>();
        
    }
}
