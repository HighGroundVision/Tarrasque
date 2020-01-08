using System;
using System.IO;
using System.Threading.Tasks;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessHeroAbilities.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.ProcessHeroAbilities
{
    public class FnProcessHeroAbilities
    {
        private readonly IProcessHeroAbilitiesService _service;

        public FnProcessHeroAbilities(IProcessHeroAbilitiesService service)
        {
            _service = service;
        }

        [FunctionName("FnProcessHeroAbilities")]
        public async Task Process(
            [QueueTrigger("hgv-hero-abilities")]HeroAbilityReference item,
            [Blob("hgv-hero-abilities/{Region}/{Date}/{Hero}/{Ability}.json")]TextReader reader,
            [Blob("hgv-hero-abilities/{Region}/{Date}/{Hero}/{Ability}.json")]TextWriter writer,
            ILogger log
        )
        {
            await _service.ProcessHeroAbility(item, reader, writer);
        }
    }
}
