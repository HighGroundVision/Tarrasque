using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class SplitRange
    {
        public TimeSpan Min { get; set; }
        public TimeSpan Max { get; set; }
    }

    public class Checkpoint
    {   
        public SplitRange Split { get; set; }
        public ulong Latest { get; set; }

        public Checkpoint()
        {
            this.Split = new SplitRange();
        }
    }
}
