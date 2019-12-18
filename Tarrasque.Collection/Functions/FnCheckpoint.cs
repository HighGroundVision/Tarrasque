using System;
using System.IO;
using System.Threading.Tasks;
using HGV.Tarrasque.Collection.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.Collection.Functions
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
            [BlobTrigger("hgv-checkpoint/master.json")]TextReader reader, 
            [Blob("hgv-checkpoint/master.json", FileAccess.Write)]TextWriter writer,
            [Queue("hgv-ad-matches")]IAsyncCollector<Models.Match> queue,
            ILogger log)
        {
            await _service.Collect(reader, writer, queue, log);
        }
    }
}
