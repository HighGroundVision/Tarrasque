using System;
using HGV.Daedalus;
using HGV.Tarrasque.Collection.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.Collection.Startup))]

namespace HGV.Tarrasque.Collection
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton<ICollectService, CollectService>();
            builder.Services.AddSingleton<ISeedService, SeedService>();
            builder.Services.AddSingleton<IProcessService, ProcessService>();
        }
    }
}
