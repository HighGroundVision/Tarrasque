using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class AggregateReference
    {
        public int Region { get; set; }
        public string Date1 { get; set; }
        public string Date2 { get; set; }
        public string Date3 { get; set; }
        public string Date4 { get; set; }
        public string Date5 { get; set; }
        public string Date6 { get; set; }
        public string Date7 { get; set; }
    }

    public class HeroAggregateReference : AggregateReference
    {
        public int Hero { get; set; }
    }

    public class AbilityAggregateReference : AggregateReference
    {
        public int Ability { get; set; }
    }
}
