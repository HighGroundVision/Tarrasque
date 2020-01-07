using HGV.Daedalus.GetMatchDetails;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class AccountReference
    {
        public long Match { get; set; }
        public long Account { get; set; }
        public long Steam { get { return Account + 76561197960265728L; } }
        public bool Victory { get; set; }
        public int Hero { get; set; }
        public List<string> Abilities { get; set; }
        public AccountReference()
        {
            this.Abilities = new List<string>();
        }
    }
}
