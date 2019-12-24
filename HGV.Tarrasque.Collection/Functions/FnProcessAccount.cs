using System;
using System.IO;
using System.Threading.Tasks;
using HGV.Tarrasque.Collection.Models;
using HGV.Tarrasque.Collection.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.Collection.Functions
{
    public class FnProcessAccount
    {

        private readonly IProcessAccountService _service;

        public FnProcessAccount(IProcessAccountService service)
        {
            _service = service;
        }

        [FunctionName("FnProcessAccount")]
        public async Task Process(
            [QueueTrigger("hgv-accounts")]AccountReference queue,
            [Blob("hgv-matches/{Match}.json")]TextReader readerMatch,
            [Blob("hgv-accounts/{Account}.json")]TextReader readerAccount,
            [Blob("hgv-accounts/{Account}.json")]TextWriter writerAccount,
            ILogger log)
        {
            if(readerMatch == null)
            {
                throw new ArgumentNullException(nameof(readerMatch), "Unable to Read Lock the Match");
            }

            await _service.UpdateAccount(queue.Account, readerMatch, readerAccount, writerAccount);
        }
    }
}
