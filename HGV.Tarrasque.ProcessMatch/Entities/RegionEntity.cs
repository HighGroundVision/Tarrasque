using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public class RegionEntity : TableEntity
    {
        public string Timestamp { get; set; }
        public int RegionId { get; set; }
        public string RegionName { get; set; }
        public int Total { get; set; }
    }
}
