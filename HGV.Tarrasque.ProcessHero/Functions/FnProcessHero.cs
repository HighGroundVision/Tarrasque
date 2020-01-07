using System;
using System.Threading.Tasks;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.ProcessHero
{
    public class FnProcessHero
    {
        private readonly IProcessRegionService _service;

        public FnProcessHero(IProcessRegionService service)
        {
            _service = service;
        }

        [FunctionName("FnProcessHero")]
        public async Task Process(
            [QueueTrigger("hgv-heroes")]HeroReference item,
            [Blob("hgv-matches/{Match}.json")]TextReader readerMatch,
            [Blob("hgv-regions/{Region}.json")]TextReader readerRegion,
            [Blob("hgv-regions/{Region}.json")]TextWriter writerRegion,
            ILogger log
        )
        {
            
        }
    }
}
