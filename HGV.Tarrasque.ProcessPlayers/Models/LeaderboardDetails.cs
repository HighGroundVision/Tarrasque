using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessPlayers.Models
{
    public class LeaderboardEntity
    {
        public int RegionId { get; set; }
        public string Persona { get; set; }
        public string Avatar { get; set; }
        public uint AccountId { get; set; }
        public ulong SteamId { get { return (ulong)AccountId + 76561197960265728L; } }
        public int Total { get; set; }
        public double WinRate { get; set; }
        public double Ranking { get; set; }  
    }

    public class LeaderboardDetails
    {
        public int Region { get; set; }
        public List<LeaderboardEntity> List { get; set; } = new List<LeaderboardEntity>();
    }
}
