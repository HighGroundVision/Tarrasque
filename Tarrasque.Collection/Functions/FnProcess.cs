using HGV.Tarrasque.Collection.Models;
using HGV.Tarrasque.Collection.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace HGV.Tarrasque.Collection.Functions
{
    public class FnProcess
    {
        private readonly IProcessService _service;

        public FnProcess(IProcessService service)
        {
            _service = service;
        }

        [FunctionName("FnProcess")]
        public void Process(
            [QueueTrigger("hgv-ad-matches")]MatchReference item,
            [Blob("hgv-matches/{MatchReference.Id}.json", FileAccess.Write)]TextWriter writerMatch,
            ILogger log)
        {
            _service.Process(item.Id, writerMatch);
        }
    }
}
