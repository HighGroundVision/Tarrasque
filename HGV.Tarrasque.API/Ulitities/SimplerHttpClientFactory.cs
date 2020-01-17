using System.Net.Http;

namespace HGV.Tarrasque.API.Ulitities
{
    public class SimplerHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return HttpClientFactory.Create();
        }
    }
}
