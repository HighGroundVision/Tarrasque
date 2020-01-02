using HGV.Daedalus;
using HGV.Tarrasque.ProcessCheckpoint.Services;
using HGV.Tarrasque.Common;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.ProcessCheckpoint.Startup))]


namespace HGV.Tarrasque.ProcessCheckpoint
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton<ICollectService, CollectService>();
        }
    }
}
