using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class TalentModel
    {
        public int TalentId { get; set; }
        public string TalentName { get; set; }
        public string Timestamp { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get { return Total - Wins; } }
        public float WinRate { get { return (float)Wins / (float)Total; } }
    }
}
