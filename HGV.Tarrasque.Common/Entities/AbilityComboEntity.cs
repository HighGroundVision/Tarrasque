using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace HGV.Tarrasque.Common.Entities
{
    public class AbilityComboEntity : TableEntity
    {
        public int PrimaryAbilityId { get; set; }
        public string PrimaryAbilityName { get; set; }
        public int ComboAbilityId { get; set; }
        public string ComboAbilityName { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public float WinRate { get; set; }
    }
}
