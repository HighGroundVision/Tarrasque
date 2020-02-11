using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.ProcessMatch.Entities
{
    public class RegionEntity : TableEntity
    {
        public int Total { get; set; }
    }
}
