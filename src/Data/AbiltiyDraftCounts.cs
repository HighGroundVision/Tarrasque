using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Data
{
    public abstract class AbiltiyDraftCounts : TableEntity
    {
        public int Picks { get; set; }
        public int Wins { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assist { get; set; }
    }

    public class AbilityCount : AbiltiyDraftCounts
    {
        public AbilityCount(int date, int ability)
        {
            this.PartitionKey = date.ToString();
            this.RowKey = ability.ToString();

            this.AbilityId = ability;

            this.Picks = 1;
        }

        public AbilityCount() { }

        public int AbilityId { get; set; }

    }

    public class ComboCount : AbiltiyDraftCounts
    {

        public ComboCount(int date, int ability1, int ability2)
        {
            var abilities = new List<int>() { ability1, ability2 };
            var key = String.Join("-", abilities.OrderBy(_ => _).ToArray());

            this.PartitionKey = date.ToString();
            this.RowKey = key;

            this.Ability1Id = ability1;
            this.Ability2Id = ability2;

            this.Picks = 1;
        }

        public ComboCount() { }

        public int Ability1Id { get; set; }
        public int Ability2Id { get; set; }
    }

    public class DraftCount : AbiltiyDraftCounts
    {
        public DraftCount(int date, int ability1, int ability2, int ability3, int ability4)
        {
            var abilities = new List<int>() { ability1, ability2, ability3, ability4 };
            var key = String.Join("-", abilities.OrderBy(_ => _).ToArray());

            this.PartitionKey = date.ToString();
            this.RowKey = key;

            this.Ability1Id = ability1;
            this.Ability2Id = ability2;
            this.Ability3Id = ability3;
            this.Ability4Id = ability4;

            this.Picks = 1;
        }

        public DraftCount() { }

        public int Ability1Id { get; set; }
        public int Ability2Id { get; set; }
        public int Ability3Id { get; set; }
        public int Ability4Id { get; set; }
    }
}
