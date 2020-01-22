using HGV.Daedalus.GetMatchDetails;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.API.Models
{
    public class AccountRef
    {
        public uint AccountId { get; set; }
        public Player Player { get; set; }
        public MatchDetails Match { get; set; }
    }
}
