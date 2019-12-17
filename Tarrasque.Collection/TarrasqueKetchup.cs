using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Tarrasque.Collection
{
    public static class TarrasqueKetchup
    {
        [FunctionName("TarrasqueKetchup")]
        public static void Run([BlobTrigger("delta/period.json")]string myBlob, ILogger log)
        {
            log.LogInformation($"Ketchup");
        }
    }
}
