﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public class HeroComboEntity : TableEntity
    {
        public string Timestamp { get; set; }
        public int HeroId { get; set; }
        public string HeroName { get; set; }
        public int AbilityId { get; set; }
        public string AbilityName { get; set; }
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public float WinRate { get; set; }
    }
}
