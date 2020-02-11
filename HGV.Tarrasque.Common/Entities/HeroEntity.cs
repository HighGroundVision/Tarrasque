using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Entities
{
    public class HeroEntity : TableEntity
    {
        public DateTime Date { get; set; }
        public int HeroId { get; set; }
        public string HeroName { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public float WinRate { get; set; }
    }
}
