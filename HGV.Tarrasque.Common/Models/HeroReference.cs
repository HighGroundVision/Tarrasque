﻿using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class HeroReference
    {
        public int DraftOrder { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int MaxAssists { get; set; }
        public int MaxGold { get; set; }
        public int MaxKills { get; set; }
        public int MinDeaths { get; set; }
    }
}