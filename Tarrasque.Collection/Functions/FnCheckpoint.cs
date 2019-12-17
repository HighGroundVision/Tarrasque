using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.Collection.Functions
{
    public class FnCheckpoint
    {
        private readonly IMyService _service;
        
        public FnCheckpoint(IMyService service)
        {
            _service = service;
        }

        [FunctionName("FnCheckpoint")]
        public async Task Checkpoint(
            [BlobTrigger("hgv-checkpoint/master.json")]TextReader reader, 
            [Blob("hgv-checkpoint/master.json", FileAccess.Write)]TextWriter writer, 
            ILogger log)
        {
            await _service.CollectMatches(reader, writer);

            log.LogInformation($"Checkpoint");
        }
    }
}
