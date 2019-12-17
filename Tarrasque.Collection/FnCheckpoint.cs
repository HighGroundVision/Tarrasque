using System;
using System.IO;
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
        public void Checkpoint([BlobTrigger("checkpoint/master.json")]Stream myBlob, ILogger log)
        {
            log.LogInformation($"Checkpoint");
        }
    }
}
