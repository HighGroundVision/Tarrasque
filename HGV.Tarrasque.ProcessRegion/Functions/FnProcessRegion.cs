using System;
using System.IO;
using System.Threading.Tasks;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessRegion.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.ProcessRegion
{
    public class FnProcessRegion
    {
        private readonly IProcessRegionService _service;

        public FnProcessRegion(IProcessRegionService service)
        {
            _service = service;
        }

        [FunctionName("FnProcessRegion")]
        public async Task Process(
            [QueueTrigger("hgv-regions")]RegionReference item,
            [Blob("hgv-matches/{Match}.json")]TextReader readerMatch,
            [Blob("hgv-regions/{Region}.json")]TextReader readerRegion,
            [Blob("hgv-regions/{Region}.json")]TextWriter writerRegion,
            ILogger log
        )
        {
            var match = await _service.ReadMatch(readerMatch);
            await _service.UpdateRegion(match, readerRegion, writerRegion);
        }
    }
}