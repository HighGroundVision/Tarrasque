using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Tarrasque.Collection
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
            [BlobTrigger("checkpoint/master.json")]TextReader reader, 
            [Blob("checkpoint/master.json", FileAccess.Write)]TextWriter writer, 
            ILogger log)
        {
            await _service.DoStuffAsync(reader, writer);

            log.LogInformation($"Checkpoint");
        }
    }
}
