using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Diagnostics.CodeAnalysis;

namespace HGV.Tarrasque.ProcessPlayers.Models
{
    public class PlayerEntity : TableEntity
    {
        public bool Calibrated { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double Ranking { get; set; }
    }
}
