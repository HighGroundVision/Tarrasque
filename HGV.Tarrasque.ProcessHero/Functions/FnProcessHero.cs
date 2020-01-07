using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessHero.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessHero
{
    public class FnProcessHero
    {
        private readonly IProcessHeroService _service;

        public FnProcessHero(IProcessHeroService service)
        {
            _service = service;
        }

        [FunctionName("FnProcessHero")]
        public async Task Process(
            [QueueTrigger("hgv-heroes")]HeroReference item,
            [Blob("hgv-heroes/{Region}/{Date}/{Hero}.json")]TextReader readHero,
            [Blob("hgv-heroes/{Region}/{Date}/{Hero}.json")]TextWriter writerHero,
            ILogger log
        )
        {
            await _service.ProcessHero(item, readHero, writerHero);
        }
    }
}
