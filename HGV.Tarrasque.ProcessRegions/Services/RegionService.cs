using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.ProcessRegions.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        Task<List<Region>> GetSummary(IBinder binder, ILogger log);
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

            T data;
            if (reader == null)
            {
                data = new T();
            }
            else
            {
                var input = await reader.ReadToEndAsync();
                data = JsonConvert.DeserializeObject<T>(input);
            }

            return data;
        }

        private static async Task WriteData<T>(IBinder binder, BlobAttribute attr, T data)
        {
            var output = JsonConvert.SerializeObject(data);
            var writer = await binder.BindAsync<TextWriter>(attr);
            await writer.WriteAsync(output);
        }

        public async Task Process(Match item, IBinder binder, ILogger log)
        {
            var regionId = this.metaClient.GetRegionId(item.cluster);
            var date = DateTimeOffset.FromUnixTimeSeconds(item.start_time);

            await UpdateSummary(binder, regionId);
            await UpdateHistory(binder, regionId, date);
        }

        private static async Task UpdateSummary(IBinder binder, int regionId)
        {
            var attr = new BlobAttribute(string.Format(SUMMARY_PATH, regionId));
            var summary = await ReadData<RegionSummary>(binder, attr);
            summary.Total++;
            await WriteData(binder, attr, summary);
        }

        private static async Task UpdateHistory(IBinder binder, int regionId, DateTimeOffset date)
        {
            var attr = new BlobAttribute(string.Format(HISTORY_PATH, regionId));
            var history = await ReadData<RegionHistory>(binder, attr);
            history.Data.Add(date);
            history.Data.RemoveAll(_ => _ < date.AddDays(-7));
            await WriteData(binder, attr, history);
        }

        public async Task<List<Region>> GetSummary(IBinder binder, ILogger log)
        {
            var collection = new List<Region>();

            var regions = metaClient.GetRegions();
            foreach (var region in regions)
            {
                var attr = new BlobAttribute(string.Format(SUMMARY_PATH, region.Key));
                var data = await ReadData<RegionSummary>(binder, attr);

                var item = new Region()
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
