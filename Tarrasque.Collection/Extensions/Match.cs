using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Collection.Extensions
{
    public static class MatchExtensions
    {
        public static DateTimeOffset GetStart(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return DateTimeOffset.FromUnixTimeSeconds(match.start_time);
        }

        public static DateTimeOffset GetEnd(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return DateTimeOffset.FromUnixTimeSeconds(match.start_time).Add(match.GetDuration());
        }

        public static TimeSpan GetDuration(this HGV.Daedalus.GetMatchDetails.Match match)
        {
            return DateTimeOffset.FromUnixTimeSeconds(match.duration).TimeOfDay;
        }

        public static DateTimeOffset GetStart(this HGV.Daedalus.GetMatchHistory.Match match)
        {
            return DateTimeOffset.FromUnixTimeSeconds(match.start_time);
        }
    }
}
