using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessPlayers.DTO
{
    public class Summary
    {
        public int RegionId { get; set; }
        public long AccountId { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double Ranking { get; set; }
        public bool Calibrated { get; set; }
    }
}
