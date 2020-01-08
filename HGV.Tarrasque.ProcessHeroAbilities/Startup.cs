using HGV.Tarrasque.ProcessHeroAbilities.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.ProcessHeroAbilities.Startup))]

namespace HGV.Tarrasque.ProcessHeroAbilities
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IProcessHeroAbilitiesService, ProcessHeroAbilitiesService>();
        }
    }
}
