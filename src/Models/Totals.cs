using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public class Totals
    {
        public Dictionary<int, int> Modes { get; set; }

        public Totals()
        {
            this.Modes = new Dictionary<int, int>();
            for (int i = 0; i < 24; i++)
            {
                this.Modes.Add(i, 0);
            }
        }
    }
}
