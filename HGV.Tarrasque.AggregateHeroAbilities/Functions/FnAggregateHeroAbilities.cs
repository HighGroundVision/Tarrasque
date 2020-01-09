using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HGV.Tarrasque.AggregateHeroAbilities.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace HGV.Tarrasque.AggregateHeroAbilities
{
    public class FnAggregateHeroAbilities
    {
        private readonly IAggregateHeroAbilitiesService _service;

        public FnAggregateHeroAbilities(IAggregateHeroAbilitiesService service)
        {
            _service = service;
        }

        [FunctionName("FnAggregateHeroAbilities")]
        public async Task Process(
            [QueueTrigger("hgv-aggregates-hero-abilities")]HeroAggregateReference item,
            [Blob("hgv-summary-heroes/{Region}/{Hero}/abilities.json")]TextWriter writer,
            IBinder binder,
            ILogger log
        )
        {
            var input = new Dictionary<int, List<TextReader>>();
            var dates = new List<string>() { item.Date1, item.Date2, item.Date3, item.Date4, item.Date5, item.Date6, item.Date7 };
            var abilities = _service.GetAbilities();

            foreach (var abilityId in abilities)
            {
                input.Add(abilityId, new List<TextReader>());

                foreach (var date in dates)
                {
                    var attr = new BlobAttribute($"hgv-hero-abilities/{item.Region}/{date}/{item.Hero}/{abilityId}.json");
                    var reader = await binder.BindAsync<TextReader>(attr);
                    if(reader != null)
                    {
                        input[abilityId].Add(reader);
                    }
                }
            }

            await _service.Process(item, input, writer);
        }
    }
}
