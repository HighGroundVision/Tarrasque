using HGV.Daedalus;
using HGV.Tarrasque.API.Services;
using HGV.Tarrasque.API.Ulitities;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.API.Startup))]
namespace HGV.Tarrasque.API
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton<IDotaService, DotaService>();
        }
    }
}
