using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Entities
{
    public class AbilityEntity : TableEntity
    {
        public DateTime Date { get; set; }
        public int AbilityId { get; set; }
        public string AbilityName { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public float WinRate { get; set; }
        public int Ancestry { get; set; }
        public int Priority { get; set; }
    }
}
