using HGV.Tarrasque.Common.Helpers;
using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.Custodian.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Custodian.Functions
{
    public class FnCustodianProcess
    {
        private readonly ICustodianService service;

        public FnCustodianProcess(ICustodianService service)
        {
            this.service = service;
        }

        [FunctionName("FnCustodianProcess")]
        public async Task Run(
            [QueueTrigger("hgv-ad-custodian")]CustodianModel model, 
            IBinder binding,
            ILogger log
        )
        {
            using (new Timer($"FnCustodianProcess", log))
            {
                // await this.service.Process(model, binding, log);
            }
        }
    }
}
