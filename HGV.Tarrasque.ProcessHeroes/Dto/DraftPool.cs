using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.ProcessHeroes.DTO
{
    public class DraftPoolAbility
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public bool IsUltimate { get; set; }
        public bool HasUpgrade { get; set; }
        public bool IsGranted { get; set; }
        public bool Enabled { get; set; }
        public string Notes { get; set; }

        // public bool HasData { get; set; }
    }

    public class DraftPoolHero
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public bool Primary { get { return Abilities.Where(_ => _.Enabled).Count() >= 4; } }
        public string ImageBanner { get; set; }
        public string ImageIcon { get; set; }
        public string ImageProfile { get; set; }
        public string Name { get; set; }
        public List<DraftPoolAbility> Abilities { get; set; } = new List<DraftPoolAbility>();
    }
}

