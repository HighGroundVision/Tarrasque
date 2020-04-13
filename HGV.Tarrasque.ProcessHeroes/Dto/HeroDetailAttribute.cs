using Dawn;
using HGV.Basilius;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace HGV.Tarrasque.ProcessHeroes.DTO
{
    public class HeroDetailAttribute
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public int Total { get; set; }
        public float Percentage { get { return Index / (float)Total;} }
        public double Value { get; set; }
    }

    public class HeroAttributeFactory
    {
        private readonly List<Hero> heroes;
        private readonly Hero hero;

        public HeroAttributeFactory(List<Hero> heroes, Hero hero)
        {
            this.heroes = heroes;
            this.hero = hero;
        }

        private HeroDetailAttribute GetAttribute(string name, Expression<Func<Hero, double>> selector)
        {
            Guard.Argument(name, nameof(name)).NotNull();
            Guard.Argument(selector, nameof(selector)).NotNull();

            var func = selector.Compile();
            var total = heroes.Select(func).Distinct().OrderBy(_ => _).ToList(); // OrderByDescending
            double value = func(hero);
            var index = total.IndexOf(value);

            var model = new HeroDetailAttribute()
            {
                Name = name,
                Index = index + 1,
                Total = total.Count + 1,
                Value = value
            };
            return model;
        }

        public List<HeroDetailAttribute> GetAttributes()
        {
            var collection = new List<HeroDetailAttribute>()
            {
                this.GetAttribute("Armor", _ => _.ArmorPhysical),
                this.GetAttribute("Damage", _ => _.AttackDamageMax),
                this.GetAttribute("Attack Range", _ => _.AttackRange),
                this.GetAttribute("Attack Rate", _ => _.AttackRate),
                this.GetAttribute("Base Agility", _ => _.AttributeBaseAgility),
                this.GetAttribute("Agility Gain", _ => _.AttributeAgilityGain),
                this.GetAttribute("Base Intelligence", _ => _.AttributeBaseIntelligence),
                this.GetAttribute("Intelligence Gain", _ => _.AttributeIntelligenceGain),
                this.GetAttribute("Base Strength", _ => _.AttributeBaseStrength),
                this.GetAttribute("Strength Gain", _ => _.AttributeStrengthGain),
                this.GetAttribute("Magical Resistance", _ => _.MagicalResistance),
                this.GetAttribute("Movement Speed", _ => _.MovementSpeed),
                this.GetAttribute("Projectile Speed", _ => _.ProjectileSpeed),
                this.GetAttribute("Health", _ => _.StatusHealth),
                this.GetAttribute("Health Regen", _ => _.StatusHealthRegen),
                this.GetAttribute("Mana", _ => _.StatusMana),
                this.GetAttribute("Mana Regen", _ => _.StatusManaRegen),
                this.GetAttribute("Vision Daytime", _ => _.VisionDaytimeRange),
                this.GetAttribute("Vision Nighttime", _ => _.VisionNighttimeRange),
            };

            return collection.OrderByDescending(_ => _.Percentage).ToList();
        }
    }
}
