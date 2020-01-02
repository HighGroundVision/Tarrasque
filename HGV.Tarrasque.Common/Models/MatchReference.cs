using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;

namespace HGV.Tarrasque.Common.Models
{
    public class MatchReference
    {
        public long Match { get; set; }

        public string Date { get; set; }

        public int Region { get; set; }

        public MatchReference(Match item)
        {
            this.Match = item.match_id;
            this.Date = item.GetStart().ToString("yy-MM-dd");
            this.Region = item.GetRegion();
        }

        public MatchReference()
        { 
        }
    }
}
