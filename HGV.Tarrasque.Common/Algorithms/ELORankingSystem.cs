using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Algorithms
{
    public static class ELORankingSystem
    {
        public static (double player, double opponent) GetExpectedScores(double player, double opponent)
        {
            double expectedScorePlayer = 1 / (1 + (Math.Pow(10, (opponent - player) / 400)));
            double expectedScoreOpponent = 1 / (1 + (Math.Pow(10, (player - opponent) / 400)));
            return (expectedScorePlayer, expectedScoreOpponent);
        }

        public static double Calucate(double ratingPlayer, double ratingOpponent, bool playerVictory, int KFACTOR = 15)
        {
            var scorePlayer = (playerVictory) ? 1.0 : 0.0;
            var expectedScores = GetExpectedScores(ratingPlayer, ratingOpponent);
            double newRatingPlayer = ratingPlayer + (KFACTOR * (scorePlayer - expectedScores.player));
            return newRatingPlayer;
        }
    }

}
