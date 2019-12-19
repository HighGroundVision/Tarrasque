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

        public static bool Victory(this HGV.Daedalus.GetMatchDetails.Match match, HGV.Daedalus.GetMatchDetails.Player player)
        {
            return (match.radiant_win && player.player_slot < 6);
        }

        public static int DraftOrder(this HGV.Daedalus.GetMatchDetails.Player player)
        {
            switch (player.player_slot)
            {
                case 0: return 0;
                case 128: return 1;
                case 1: return 2;
                case 129: return 3;
                case 2: return 4;
                case 130: return 5;
                case 3: return 6;
                case 131: return 7;
                case 4: return 8;
                case 132: return 9;
                default: return 0;
            }
        }
    }
}
