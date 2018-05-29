using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class AccountLeaseData
    {
        public int game_mode { get; set; }
        public long dota_id { get; set; }
        public DateTime expiry { get; set; }
    }
}
