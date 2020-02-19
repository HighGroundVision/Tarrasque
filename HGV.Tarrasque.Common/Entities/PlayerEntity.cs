﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Entities
{
    public class PlayerEntity : TableEntity
    {
        public long AccountId { get; set; }
        public long SteamId { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
        public double Ranking { get; set; }
    }
}
