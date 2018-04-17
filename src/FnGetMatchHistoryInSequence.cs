using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using HGV.Daedalus;

namespace HGV.Tarrasque.Functions
{
    public static class FnGetMatchHistoryInSequence
    {
        [FunctionName("GetMatchHistoryInSequence")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [BlobTrigger("hgv-master/next.json")]TextReader tirggerBlob,
            [Blob("hgv-master/config.json", System.IO.FileAccess.Read)]TextReader inputBlog,
            [Blob("hgv-master/next.json", System.IO.FileAccess.ReadWrite)]TextWriter outputBlob,
            [Queue("hgv-matches-history")]ICollector<string> matchBlob,
            TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var config = inputBlog.ReadToEnd();
            var body = tirggerBlob.ReadToEnd();

            /*
            using (var client = new DotaApiClient(""))
            { 

            }
            */

            matchBlob.Add(body);

            outputBlob.Write(body);
        }
    }
}
