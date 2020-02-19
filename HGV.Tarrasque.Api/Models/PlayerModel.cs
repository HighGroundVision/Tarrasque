using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Api.Models
{
    public class PlayerModel
    {
        public int RegionId { get; set; }
        public long AccountId { get; set; }
        public long SteamId { get; set; }
        public string Persona { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
        public double Ranking { get; set; }
    }
}
