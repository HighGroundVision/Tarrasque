using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class VoteData
    {
        public int account_id { get; set; }
        public int type { get; set; }
        public string key { get; set; }
        public bool vote { get; set; }
    }
}
