using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HGV.Daedalus;
using Newtonsoft.Json;

namespace Tarrasque.Collection
{
    public interface IMyService
    {
        Task Seed(TextWriter writer);
        Task CollectMatches(TextReader reader, TextWriter writer);
    }

    public class MyService : IMyService
    {
        private readonly HttpClient _httpClient;
        private readonly DotaApiClient _dotaClient;

        public MyService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        private async Task<List<long>> GetLatest()
        {
            var url = string.Format("https://api.steampowered.com/IDOTA2Match_570/GetMatchHistory/V001/?key={0}", "262549766180623D9E3E511E1E2168A5");
            var json = await _httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<HGV.Daedalus.GetMatchHistory.GetMatchHistoryResult>(json);
            var collection = data?.result?.matches ?? new List<HGV.Daedalus.GetMatchHistory.Match>();
            var matches = collection.Select(_ => _.match_seq_num).ToList();
            return matches;
        }

        public async Task Seed(TextWriter writer)
        {
            var matches = await GetLatest();
            var model = new Models.Checkpoint()
            {
                History = matches,
                Counter = 1,
            };
            var json = JsonConvert.SerializeObject(model);
            await writer.WriteAsync(json);
        }

        public async Task CollectMatches(TextReader reader, TextWriter writer)
        {
            var json = await reader.ReadToEndAsync();


            await writer.WriteAsync(json);

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
