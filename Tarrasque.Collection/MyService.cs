using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Tarrasque.Collection
{
    public interface IMyService
    {
        Task DoStuffAsync(TextReader reader, TextWriter writer);
    }

    public class MyService : IMyService
    {
        private readonly HttpClient _client;

        public MyService(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }

        public async Task DoStuffAsync(TextReader reader, TextWriter writer)
        {
            var res = await _client.GetAsync("http://www.google.ca");
            var result = await res.Content.ReadAsStringAsync();

            var json = await reader.ReadToEndAsync();
            await writer.WriteAsync(json);

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
