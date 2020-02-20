using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Api.Models
{
    public class HeroHistory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Current { get; set; }
        public double Previous { get; set; }
    }
}
