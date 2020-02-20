using Dawn;
using HGV.Basilius;
using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Common.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Api.Services
{
    public interface IHeroService
    {
        List<HeroCategory> GetHeroCategories();
        Task<List<HeroHistory>> GetHeroHistory(string start, string end, CloudTable table, ILogger log);
        Task<HeroDetails> GetHeroDetails(int id, IBinder binding, ILogger log);
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

        public async Task<List<HeroHistory>> GetHeroHistory(string start, string end, CloudTable table, ILogger log)
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

        public async Task<HeroDetails> GetHeroDetails(int id, IBinder binding, ILogger log)
        {
            var heroes = MetaClient.Instance.Value.GetHeroes();
            var hero = heroes.Find(_ => _.Id == id);
            if (hero == null)
                throw new ArgumentOutOfRangeException(nameof(id));

            var factory = new HeroAttributeFactory(heroes, hero);
            var model = new HeroDetails();
            model.Id = hero.Id;
            model.Name = hero.Name;
            model.Image = hero.ImageBanner;
            model.Primary = hero.AttributePrimary;
            model.Enabled = hero.AbilityDraftEnabled;
            model.Attributes = factory.GetAttributes();
            model.History = await GetHeroHistory(id, binding, log);

            // HeroComboEntity

            return model;
        }

        private async Task<List<HeroDetailsHistory>> GetHeroHistory(int id, IBinder binding, ILogger log)
        {
            var table = await binding.BindAsync<CloudTable>(new TableAttribute("HGVHeroes"));
            var filter = string.Empty;
            var dates = Enumerable.Range(1, 6).Select(_ => DateTime.UtcNow.AddDays(_ * -1).ToString("yy-MM-dd")).ToList();
            foreach (var date in dates)
            {
                if(string.IsNullOrWhiteSpace(filter))
                {
                    filter = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, date),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, $"{id}")
                    );
                }
                else
                {
                    var condition = TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, date),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, $"{id}")
                    );
                    filter = TableQuery.CombineFilters(filter, TableOperators.Or, condition);
                }
                
            }

            var query = new TableQuery<HeroEntity>().Where(filter);
            var collection = new List<HeroDetailsHistory>();
            TableContinuationToken? token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync<HeroEntity>(query, token);
                token = segment.ContinuationToken;
                var results = segment.Results.Select(_ => new HeroDetailsHistory() 
                {
                    Date = _.PartitionKey,
                    Total = _.Total, 
                    Wins = _.Wins, 
                    Losses = _.Losses, 
                    WinRate = _.WinRate 
                });
                collection.AddRange(results);
            }
            while (token != null);

            return collection.OrderByDescending(_ => _.Date).ToList();
        }
    }
}
