using HGV.Basilius;
using HGV.Daedalus;
using HGV.Tarrasque.Common;
using HGV.Tarrasque.ProcessaAbilities.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.ProcessaAbilities.Startup))]

namespace HGV.Tarrasque.ProcessaAbilities
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton(MetaClient.Instance.Value);

            builder.Services.AddSingleton<IAbilityService, AbilityService>();
        }
    }
}
