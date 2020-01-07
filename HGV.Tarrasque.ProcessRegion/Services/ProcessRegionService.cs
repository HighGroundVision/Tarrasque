using HGV.Basilius;
using HGV.Daedalus;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Extensions;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessRegion.Services
{
    public interface IProcessRegionService
    {
        Task<Match> ReadMatch(TextReader reader);
        Task UpdateRegion(Match match, TextReader reader, TextWriter writer);
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
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var input = await reader.ReadToEndAsync();
            var match = JsonConvert.DeserializeObject<Match>(input);
            return match;
        }

        public async Task UpdateRegion(Match match, TextReader reader, TextWriter writer)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            Func<RegionData> init = () =>
            {
                return new RegionData(match);
            };

            Action<RegionData> update = _ =>
            {
                var date = match.GetStart().Date;
                if (_.Range.ContainsKey(date))
                    _.Range[date]++;
                else
                    _.Range.Add(date, 1);
            };

            await this.ReadUpdateWriteHandler(reader, writer, init, update);
        }

        private async Task ReadUpdateWriteHandler<T>(TextReader reader, TextWriter writer, Func<T> init, Action<T> update) where T : class
        {
            if (reader == null)
                reader = new StringReader(string.Empty);

            var input = await reader.ReadToEndAsync();
            var data = string.IsNullOrWhiteSpace(input) ? init() : JsonConvert.DeserializeObject<T>(input);

            update(data);

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }
    }
}
