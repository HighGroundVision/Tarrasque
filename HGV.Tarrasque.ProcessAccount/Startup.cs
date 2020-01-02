using HGV.Daedalus;
using HGV.Tarrasque.ProcessAccount.Services;
using HGV.Tarrasque.Common;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.ProcessAccount.Startup))]

namespace HGV.Tarrasque.ProcessAccount
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton<IProcessAccountService, ProcessAccountService>();
        }
    }
}
