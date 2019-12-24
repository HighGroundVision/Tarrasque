using HGV.Daedalus.GetMatchDetails;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{

    public class WinRateData
    {
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }

    public class AccountData : WinRateData
    {
        public long AccountId { get; set; }
        public long SteamId { get; set; }
        public string Persona { get; set; }

        public IDictionary<int, WinRateData> Heroes { get; set; }
        public IDictionary<string, WinRateData> Abilities { get; set; }
        public IList<long> Matches { get; set; }

        public AccountData()
        {
            this.Matches = new List<long>();
            this.Heroes = new Dictionary<int, WinRateData>();
            this.Abilities = new Dictionary<string, WinRateData>();
        }

        public void AddHeroWin(int id)
        {
            if(this.Heroes.ContainsKey(id))
            {
                this.Heroes[id].Wins++;
                this.Heroes[id].Total++;
            }
            else
            {
                this.Heroes.Add(id, new WinRateData() { Total = 1, Wins = 1, Losses = 0 });
            }
        }
        public void AddHeroLose(int id)
        {
            if (this.Heroes.ContainsKey(id))
            {
                this.Heroes[id].Losses++;
                this.Heroes[id].Total++;
            }
            else
            {
                this.Heroes.Add(id, new WinRateData() { Total = 1, Wins = 0, Losses = 1 });
            }
        }

        public void AddAbilitiesWin(List<string> upgrades)
        {
            foreach (var id in upgrades)
            {
                if (this.Abilities.ContainsKey(id))
                {
                    this.Abilities[id].Wins++;
                    this.Abilities[id].Total++;
                }
                else
                {
                    this.Abilities.Add(id, new WinRateData() { Total = 1, Wins = 1, Losses = 0 });
                }
            }
        }

        public void AddAbilitiesLose(List<string> upgrades)
        {
            foreach (var id in upgrades)
            {
                if (this.Abilities.ContainsKey(id))
                {
                    this.Abilities[id].Losses++;
                    this.Abilities[id].Total++;
                }
                else
                {
                    this.Abilities.Add(id, new WinRateData() { Total = 1, Wins = 0, Losses = 1 });
                }
            }
        }
    }
}
