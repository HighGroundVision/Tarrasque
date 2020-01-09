using HGV.Tarrasque.AggregateAbility.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.AggregateAbility
{
    public class FnAggregateAbility
    {
        private readonly IAggregateAbilityService _service;

        public FnAggregateAbility(IAggregateAbilityService service)
        {
            _service = service;
        }

        [FunctionName("FnAggregateAbility")]
        public async Task Run(
            [QueueTrigger("hgv-aggregates-abilities")]AbilityAggregateReference item,
            [Blob("hgv-abilities/{Region}/{Date1}/{Ability}.json")]TextReader day1,
            [Blob("hgv-abilities/{Region}/{Date2}/{Ability}.json")]TextReader day2,
            [Blob("hgv-abilities/{Region}/{Date3}/{Ability}.json")]TextReader day3,
            [Blob("hgv-abilities/{Region}/{Date4}/{Ability}.json")]TextReader day4,
            [Blob("hgv-abilities/{Region}/{Date5}/{Ability}.json")]TextReader day5,
            [Blob("hgv-abilities/{Region}/{Date6}/{Ability}.json")]TextReader day6,
            [Blob("hgv-abilities/{Region}/{Date7}/{Ability}.json")]TextReader day7,
            [Blob("hgv-summary-abilities/{Region}/{Ability}.json")]TextWriter writer,
            ILogger log
        )
        {
            var readers = new List<TextReader>() { day1, day2, day3, day4, day5, day6, day7 };
            await _service.Process(item, readers, writer);
        }
    }
}
