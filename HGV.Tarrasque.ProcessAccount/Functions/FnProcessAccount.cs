using System;
using System.IO;
using System.Threading.Tasks;
using HGV.Tarrasque.ProcessAccount.Services;
using HGV.Tarrasque.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.ProcessAccount.Functions
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
            [QueueTrigger("hgv-accounts")]AccountReference item,
            [Blob("hgv-accounts/{Account}.json")]TextReader readerAccount,
            [Blob("hgv-accounts/{Account}.json")]TextWriter writerAccount,
            ILogger log)
        {
            await _service.ProcessAcount(item, readerAccount, writerAccount);
        }
    }
}
