using HGV.Daedalus;
using HGV.Tarrasque.Common;
using HGV.Tarrasque.ProcessMatch.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.ProcessMatch.Startup))]

namespace HGV.Tarrasque.ProcessMatch
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton<IProcessMatchService, ProcessMatchService>();
            builder.Services.AddSingleton<IProcessPlayersService, ProcessPlayersService>();
        }
    }
}
