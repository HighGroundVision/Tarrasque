using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class RecentMatch
    {
        public long match_id { get; set; }
        public long match_number { get; set; }
        public int slot { get; set; }
        public int team { get; set; }
        public bool won { get; set; }
        public long start_time { get; set; }
        public int duration { get; set; }
        public int game_mode { get; set; }
        public int hero_id { get; set; }
        public int kills { get; set; }
        public int deaths { get; set; }
        public int assists { get; set; }
        public int last_hits { get; set; }
        public int level { get; set; }
        public List<int> abilities { get; set; }

        public RecentMatch()
        {
            this.abilities = new List<int>();
        }
    }
}
