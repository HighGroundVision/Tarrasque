using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessPlayers.DTO
{
    public class Leaderboard
    {
        public uint AccountId { get; set; }
        public ulong SteamId { get; set; }
        public string Persona { get; set; }
        public string Avatar { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double Ranking { get; set; }
        public string RegionName { get; set; }
        public string RegionGroup { get; set; }
    }
}
