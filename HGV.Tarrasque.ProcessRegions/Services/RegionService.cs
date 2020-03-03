using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Exceptions;
using HGV.Tarrasque.ProcessRegions.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessRegions.Services
{
    public interface IRegionService
    {
        Task Process(Match item, IBinder binder, ILogger log);
        Task<List<DTO.Region>> GetSummary(IBinder binder, ILogger log);
        Task<Dictionary<string, int>> GetHistory(int regionId, IBinder binder, ILogger log);
    }

    public class RegionService : IRegionService
    {
        private const string SUMMARY_PATH = "hgv-regions/{0}/summary.json";
        private const string HISTORY_PATH = "hgv-regions/{0}/history.json";
        private const string HISTORY_DELIMITER = "yy-MM-dd";

        private readonly MetaClient metaClient;

        public RegionService(MetaClient metaClient)
        {
            this.metaClient = metaClient;
        }

        private static async Task<T> ReadData<T>(IBinder binder, BlobAttribute attr) where T : new()
        {
            var reader = await binder.BindAsync<TextReader>(attr);
            if (reader == null)
                throw new NotFoundException();

            var input = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(input))
                throw new NotFoundException();

            return JsonConvert.DeserializeObject<T>(input);
        }

        private async Task ProcessBlob<T>(CloudBlockBlob blob, Action<T> updateFn, ILogger log) where T : new()
        {
            try
            {
                var exist = await blob.ExistsAsync();
                if (!exist)
                {
                    var data = new T();
                    var json = JsonConvert.SerializeObject(data);
                    await blob.UploadTextAsync(json);
                }
            }
            catch (Exception)
            {
            }

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync( (n) => TimeSpan.FromMilliseconds(100));

            var ac = await policy.ExecuteAsync(async () => 
            {
                var leaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(30));
                if (string.IsNullOrEmpty(leaseId))
                    throw new NullReferenceException();

                return AccessCondition.GenerateLeaseCondition(leaseId);
            });

            try
            {
                var input = await blob.DownloadTextAsync(ac, null, null);
                var data = JsonConvert.DeserializeObject<T>(input);
                updateFn(data);
                var output = JsonConvert.SerializeObject(data);
                await blob.UploadTextAsync(output, ac, null, null);
            }
            finally
            {
                await blob.ReleaseLeaseAsync(ac, null, null);
            }
        }

        public async Task Process(Match item, IBinder binder, ILogger log)
        {
            var regionId = this.metaClient.GetRegionId(item.cluster);
            var date = DateTimeOffset.FromUnixTimeSeconds(item.start_time);

            await UpdateSummary(binder, regionId, log);
            await UpdateHistory(binder, regionId, date, log);
        }

        private async Task UpdateSummary(IBinder binder, int regionId, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(SUMMARY_PATH, regionId));
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            Action<RegionSummary> updateFn = (summary) => summary.Total++;
            await ProcessBlob(blob, updateFn, log);
        }
   
        private async Task UpdateHistory(IBinder binder, int regionId, DateTimeOffset date, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(HISTORY_PATH, regionId));
            var blob = await binder.BindAsync<CloudBlockBlob>(attr);

            Action<RegionHistory> updateFn = (history) =>
            {
                history.Data.Add(date);
                history.Data.RemoveAll(_ => _ < date.AddDays(-7));
            };

            await ProcessBlob(blob, updateFn, log);
        }

        public async Task<List<DTO.Region>> GetSummary(IBinder binder, ILogger log)
        {
            var collection = new List<DTO.Region>();

            var regions = metaClient.GetRegions();
            foreach (var region in regions)
            {
                var attr = new BlobAttribute(string.Format(SUMMARY_PATH, region.Key));
                var data = await ReadData<RegionSummary>(binder, attr);

                var item = new DTO.Region()
                {
                    Id = region.Key,
                    Name  = region.Value,
                    Total = data.Total,
                };
                collection.Add(item);
            }

            return collection;
        }

        public async Task<Dictionary<string, int>> GetHistory(int regionId, IBinder binder, ILogger log)
        {
            var attr = new BlobAttribute(string.Format(HISTORY_PATH, regionId));
            var history = await ReadData<RegionHistory>(binder, attr);
            var collection = history.Data
                .GroupBy(_ => _.ToString(HISTORY_DELIMITER))
                .Select(_ => new { Region = _.Key, Total = _.Count() })
                .ToDictionary(_ => _.Region, _ => _.Total);

            return collection;
        }
    }
}
