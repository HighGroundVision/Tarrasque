using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;

namespace HGV.Tarrasque.Common.Models
{
    public class MatchReference
    {
        public ulong Match { get; set; }

        public string Date { get; set; }

        public int Region { get; set; }
    }
}
