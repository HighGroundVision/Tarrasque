using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Tarrasque.Collection
{
    public class Model
    {
        public int Counter { get; set; }
    }

    public static class TarrasqueCollection
    {
        [FunctionName(nameof(TarrasqueCollection))]
        public static void Run(
            [BlobTrigger("checkpoint/master.json")]TextReader reader, 
            [Blob("checkpoint/master.json", FileAccess.Write)]TextWriter writer, 
            [Blob("delta/period.json", FileAccess.Write)]TextWriter delta, ILogger log)
        {
            var json = reader.ReadToEnd();
            var model = JsonConvert.DeserializeObject<Model>(json);
            model.Counter++;
            json = JsonConvert.SerializeObject(model);
            writer.Write(json);
        }
    }
}
