using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class AccountRefreshMessage
    {
        public int game_mode { get; set; }
        public long dota_id { get; set; }
    }
}
