using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Entities
{
    public class RegionEntity : TableEntity
    {
        public int RegionId { get; set; }
        public string RegionName { get; set; }
        public int Total { get; set; }
    }
}
