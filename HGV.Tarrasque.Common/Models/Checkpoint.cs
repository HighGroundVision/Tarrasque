using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{
    public class Checkpoint
    {   
        public TimeSpan Split { get; set; }
        public ulong Latest { get; set; }
    }
}
