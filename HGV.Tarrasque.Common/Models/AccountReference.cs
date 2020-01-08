using HGV.Daedalus.GetMatchDetails;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class AccountReference
    {
        public ulong Match { get; set; }
        public uint Account { get; set; }
        public ulong Steam { get { return (ulong)Account + 76561197960265728L; } }
        public bool Victory { get; set; }
        public int Hero { get; set; }
        public List<int> Abilities { get; set; }

        public AccountReference()
        {
            this.Abilities = new List<int>();
        }
    }
}
