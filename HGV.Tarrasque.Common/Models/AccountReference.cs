using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class AccountReference
    {
        public long Account { get; set; }
        public long Match { get; set; }

        public long Steam { get { return Account + 76561197960265728L; } }
    }
}
