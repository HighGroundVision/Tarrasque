using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Entities
{
    public class PlayerEntity : TableEntity
    {
        public ulong AccountId { get; set; }
        public ulong SteamId { get; set; }
        public string Persona { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public float WinRate { get; set; }
        public double Ranking { get; set; }
    }
}
