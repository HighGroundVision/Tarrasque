using HGV.Tarrasque.AggregateHero.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.AggregateHero.Startup))]

namespace HGV.Tarrasque.AggregateHero
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IAggregateHeroService, AggregateHeroService>();
        }
    }
}
