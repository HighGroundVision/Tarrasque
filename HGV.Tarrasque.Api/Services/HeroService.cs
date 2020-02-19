using HGV.Basilius;
using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Common.Entities;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Services
{
    public interface IHeroService
    {
        List<HeroCategory> GetHeroCategories();
        Task<List<HeroHistory>> GetHeroHistory(CloudTable table, string start, string end);
    }

    public class HeroService : IHeroService
    {
        public List<HeroCategory> GetHeroCategories()
        {
            var heroes = MetaClient.Instance.Value.GetADHeroes();
            var collection = heroes.Select(_ => new HeroCategory()
            {
                Name = _.Name,
                Image = _.ImageIcon
            })
            .ToList();

            return collection;
        }

        public async Task<List<HeroHistory>> GetHeroHistory(CloudTable table, string start, string end)
        {
            var collectionCurrent = new List<HeroEntity>();
            {
                
                var query = new TableQuery<HeroEntity>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, start)
                );
                TableContinuationToken token = null;
                do
                {
                    var segment = await table.ExecuteQuerySegmentedAsync<HeroEntity>(query, token);
                    token = segment.ContinuationToken;
                    collectionCurrent.AddRange(segment.Results);
                }
                while (token != null);
            }
            var collectionPreivous = new List<HeroEntity>();
            {
                
                var query = new TableQuery<HeroEntity>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, end)
                );
                TableContinuationToken token = null;
                do
                {
                    var segment = await table.ExecuteQuerySegmentedAsync<HeroEntity>(query, token);
                    token = segment.ContinuationToken;
                    collectionPreivous.AddRange(segment.Results);
                }
                while (token != null);
            }

            var heroes = MetaClient.Instance.Value.GetADHeroes()
                .Select(_ => new { _.Id, _.Name })
                .ToList();

            var collection = heroes
                .GroupJoin(collectionCurrent, _ => _.Id, _ => _.HeroId, (hero, data) => new { hero, data })
                .SelectMany(_ => _.data.DefaultIfEmpty(), (x, data) => new { Hero = x.hero, Current = data })
                .GroupJoin(collectionPreivous, _ => _.Hero.Id, _ => _.HeroId, (meta, data) => new { meta, data })
                .SelectMany(_ => _.data.DefaultIfEmpty(), (x, data) => new { Hero = x.meta.Hero, Current = x.meta.Current, Previous = data })
                .Select(_ => new HeroHistory()
                {
                    Id = _.Hero.Id,
                    Name = _.Hero.Name,
                    Current = _.Current?.WinRate ?? 0f,
                    Previous = _.Previous?.WinRate ?? 0f
                })
                .ToList();

            return collection;
        }
    }
}
