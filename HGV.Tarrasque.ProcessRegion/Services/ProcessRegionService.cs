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
        Task<Match> ReadMatch(TextReader reader);
        Task ProcessRegion(Match match, TextReader reader, TextWriter writer);
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

        public async Task ProcessRegion(Match match, TextReader reader, TextWriter writer)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            if (reader == null)
                await NewRegion(match, writer);
            else
                await UpdateRegion(match, reader, writer);
        }

        private static async Task NewRegion(Match match, TextWriter writer)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var data = new RegionData();
            data.Id = match.GetRegion();

            var date = match.GetStart().Date;
            data.Range.Add(date, 1);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

        private static async Task UpdateRegion(Match match, TextReader reader, TextWriter writer)
        {
            Guard.Argument(match, nameof(match)).NotNull();
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            var input = await reader.ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<RegionData>(input);

            var date = match.GetStart().Date;
            if (data.Range.ContainsKey(date))
                data.Range[date]++;
            else
                data.Range.Add(date, 1);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

       
    }
}
