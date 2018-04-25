using Microsoft.WindowsAzure.Storage.Table;

namespace HGV.Tarrasque.Models
{
    public class AbilityStats : TableEntity
    {
        public int Type { get; set; }

        public int Wins { get; set; }
        public int Picks { get; set; }

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Damage { get; set; }         // Damage To Heroes
        public int Destruction { get; set; }    // Damage To Structures
        public int Gold { get; set; }

        public double PickVsTotal { get; set; }
        public double WinsVsTotal { get; set; }
        public double WinsVsPicks { get; set; }
    }
}
