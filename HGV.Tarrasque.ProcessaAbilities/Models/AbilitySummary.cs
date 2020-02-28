using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessAbilities.Models
{
    public class AbilitySummary
    {
        public Dictionary<int, AbilityHistoryData> Data { get; set; } = new Dictionary<int, AbilityHistoryData>();
    }
}
