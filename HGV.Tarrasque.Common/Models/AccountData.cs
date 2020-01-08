using HGV.Daedalus.GetMatchDetails;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Common.Models
{

    public class WinRateData
    {
        public int Total { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }

    public class AccountData : WinRateData
    {
        public uint AccountId { get; set; }
        public ulong SteamId { get; set; }
        public string Persona { get; set; }
        public string Avatar { get; set; }

        public IDictionary<int, WinRateData> Heroes { get; set; }
        public IDictionary<int, WinRateData> Abilities { get; set; }
        public IList<ulong> Matches { get; set; }

        public AccountData()
        {
            this.Persona = string.Empty;
            this.Avatar = string.Empty;
            this.Matches = new List<ulong>();
            this.Heroes = new Dictionary<int, WinRateData>();
            this.Abilities = new Dictionary<int, WinRateData>();
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

        public void AddAbilitiesWin(List<int> upgrades)
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

        public void AddAbilitiesLose(List<int> upgrades)
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
