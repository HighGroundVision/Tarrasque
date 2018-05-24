using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class PlayersAbilities
    {
        public long account_id { get; set; }
        public List<int> abilities { get; set; }

        public PlayersAbilities()
        {
            account_id = 0;
            abilities = new List<int>();
        }
    }
}
