using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessHeroes.Models
{
    public class HeroSummary
    {
        public Dictionary<int, HeroHistoryData> Data { get; set; } = new Dictionary<int, HeroHistoryData>();
    }
}
