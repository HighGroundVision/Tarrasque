using HGV.Tarrasque.ProcessCheckpoint.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

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
            [Blob("hgv-checkpoint/history.json")]TextReader readerHistory,
            [Blob("hgv-checkpoint/history.json")]TextWriter writerHistory,
            [Queue("hgv-ad-matches")]IAsyncCollector<MatchReference> queue,
            ILogger log)
        {
            if (readerCheckpoint == null)
                throw new ArgumentNullException(nameof(readerCheckpoint));
            
            if (writerCheckpoint == null)
                throw new ArgumentNullException(nameof(writerCheckpoint));

            if (readerHistory == null)
                throw new ArgumentNullException(nameof(readerHistory));

            if (writerHistory == null)
                throw new ArgumentNullException(nameof(writerHistory));

            await _service.Collect(readerCheckpoint, writerCheckpoint, readerHistory, writerHistory, queue);
        }
    }
}
