using HGV.Tarrasque.ProcessCheckpoint.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using HGV.Daedalus.GetMatchDetails;

namespace HGV.Tarrasque.ProcessCheckpoint.Functions
{
    public class FnCheckpoint
    {
        private readonly ICollectService _service;
        
        public FnCheckpoint(ICollectService service)
        {
            _service = service;
        }

        [FunctionName("FnCheckpoint")]
        public async Task Checkpoint(
            [BlobTrigger("hgv-checkpoint/master.json")]TextReader readerCheckpoint,
            [Blob("hgv-checkpoint/master.json")]TextWriter writerCheckpoint,
            [Queue("hgv-ad-matches")]IAsyncCollector<Match> queue,
            ILogger log)
        {
            await _service.Collect(readerCheckpoint, writerCheckpoint, queue, log);
        }
    }
}
