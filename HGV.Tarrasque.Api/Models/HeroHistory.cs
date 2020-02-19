using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Api.Models
{
    public class HeroHistory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Current { get; set; }
        public float Previous { get; set; }
    }
}
