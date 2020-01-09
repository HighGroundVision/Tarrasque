using Dawn;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessRegion.Services
{
    public interface IProcessRegionService
    {
        Task ProcessRegion(RegionReference item, TextReader reader, TextWriter writer);
    }

    public class ProcessRegionService : IProcessRegionService
    {
        public ProcessRegionService()
        {
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
            data.Date = item.Date;
            data.Matches = 1;

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

            data.Matches++;

            var output = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(output);
        }

       
    }
}
