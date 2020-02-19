using HGV.Tarrasque.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Api.Models
{
    public class HeroDetails
    {
        // Hero
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public bool Enabled { get; set; }
        public string Primary { get; set; }
        public List<HeroDetailsAttribute> Attributes { get; set; } = new List<HeroDetailsAttribute>();
        public List<HeroDetailsHistory> History { get; set; } = new List<HeroDetailsHistory>();

        // Heroes Default Abilities
        public List<object> Abilities { get; set; } = new List<object>();

        // Heroes Talents
        // (List details add win rate later)
        public List<object> Talents { get; set; } = new List<object>();

        // Heroes Combos
        public List<object> Combos { get; set; } = new List<object>();
    }
}
