using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Dawn;

namespace HGV.Tarrasque.ProcessRegion.Services
{
    public interface IProcessRegionService
    {
        Task ProcessRegion(RegionReference item, TextReader reader, TextWriter writer);
    }

    public class ProcessRegionService : IProcessRegionService
    {
        private readonly IDotaApiClient apiClient;
        private readonly MetaClient metaClient;

        public ProcessRegionService(IDotaApiClient client)
        {
            this.apiClient = client;
            this.metaClient = MetaClient.Instance.Value;
        }

        public async Task<Match> ReadMatch(TextReader reader)
        {
            Guard.Argument(reader, nameof(reader)).NotNull();

            var input = await reader.ReadToEndAsync();
            var match = JsonConvert.DeserializeObject<Match>(input);
            return match;
        }

        public async Task ProcessRegion(RegionReference regionRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(regionRef, nameof(regionRef)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            if (reader == null)
                await NewRegion(regionRef, writer);
            else
                await UpdateRegion(regionRef, reader, writer);
        }

        private static async Task NewRegion(RegionReference item, TextWriter writer)
        {
            Guard.Argument(item, nameof(item)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var data = new RegionData();
            data.Id = item.Region;
            data.Range.Add(item.Date, 1);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static async Task UpdateRegion(RegionReference regionRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(regionRef, nameof(regionRef)).NotNull();
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var input = await reader.ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<RegionData>(input);

            if (data.Range.ContainsKey(regionRef.Date))
                data.Range[regionRef.Date]++;
            else
                data.Range.Add(regionRef.Date, 1);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

       
    }
}
