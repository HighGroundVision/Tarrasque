using HGV.Tarrasque.Common.Models;
using HGV.Tarrasque.ProcessAbility.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessAbility
{
    public class FnProcessAbility
    {
        private readonly IProcessAbilitiesService _service;

        public FnProcessAbility(IProcessAbilitiesService service)
        {
            _service = service;
        }

        [FunctionName("FnProcessAbility")]
        public async Task Process(
            [QueueTrigger("hgv-abilities")]AbilityReference item,
            [Blob("hgv-abilities/{Region}/{Date}/{Ability}.json")]TextReader reader,
            [Blob("hgv-abilities/{Region}/{Date}/{Ability}.json")]TextWriter writer,
            ILogger log
        )
        {
            await _service.ProcessAbility(item, reader, writer);
        }
    }
}
