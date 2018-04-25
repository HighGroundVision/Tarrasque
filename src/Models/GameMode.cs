using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Models
{
    public enum GameMode
    {
        unknown = 0,
        all_pick = 1,
        captains_mode = 2,
        random_draft = 3,
        single_draft = 4,
        all_random = 5,
        intro = 6,
        diretide = 7,
        reverse_captains_mode = 8,
        greeviling = 9,
        tutorial = 10,
        mid_only = 11,
        least_played = 12,
        limited_heroes = 13,
        compendium_matchmaking = 14,
        custom = 15,
        captains_draft = 16,
        balanced_draft = 17,
        ability_draft = 18,
        all_random_death_match = 20,
        mid_1v1 = 21,
        all_draft = 22,
        turbo = 23,
    }
}
