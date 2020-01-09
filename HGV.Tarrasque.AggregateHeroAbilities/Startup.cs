using HGV.Tarrasque.AggregateHeroAbilities.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.AggregateHeroAbilities.Startup))]


namespace HGV.Tarrasque.AggregateHeroAbilities
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IAggregateHeroAbilitiesService, AggregateHeroAbilitiesService>();
        }
    }
}
