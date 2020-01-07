using HGV.Daedalus;
using HGV.Tarrasque.Common;
using HGV.Tarrasque.ProcessRegion.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.ProcessRegion.Startup))]

namespace HGV.Tarrasque.ProcessRegion
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton<IProcessRegionService, ProcessRegionService>();
        }
    }
}
