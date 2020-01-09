using HGV.Tarrasque.AggregateHero.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.AggregateHero
{
    public class FnAggregateHero
    {
        private readonly IAggregateHeroService _service;

        public FnAggregateHero(IAggregateHeroService service)
        {
            _service = service;
        }

        [FunctionName("FnAggregateHero")]
        public async Task Process(
            [QueueTrigger("hgv-aggregates-heroes")]HeroAggregateReference item,
            [Blob("hgv-heroes/{Region}/{Date1}/{Hero}.json")]TextReader day1,
            [Blob("hgv-heroes/{Region}/{Date2}/{Hero}.json")]TextReader day2,
            [Blob("hgv-heroes/{Region}/{Date3}/{Hero}.json")]TextReader day3,
            [Blob("hgv-heroes/{Region}/{Date4}/{Hero}.json")]TextReader day4,
            [Blob("hgv-heroes/{Region}/{Date5}/{Hero}.json")]TextReader day5,
            [Blob("hgv-heroes/{Region}/{Date6}/{Hero}.json")]TextReader day6,
            [Blob("hgv-heroes/{Region}/{Date7}/{Hero}.json")]TextReader day7,
            [Blob("hgv-summary-heroes/{Region}/{Hero}/stats.json")]TextWriter writer,
            ILogger log
        )
        {
            var readers = new List<TextReader>() { day1, day2, day3, day4, day5, day6, day7 };
            await _service.Process(item, readers, writer);
        }
    }
}
