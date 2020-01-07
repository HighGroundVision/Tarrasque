using HGV.Daedalus;
using HGV.Tarrasque.Common;
using HGV.Tarrasque.StoreMatch.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.StoreMatch.Startup))]

namespace HGV.Tarrasque.StoreMatch
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton<IStoreMatchService, StoreMatchService>();
        }
    }
}
