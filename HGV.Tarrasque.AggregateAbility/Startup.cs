using HGV.Tarrasque.AggregateAbility.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.AggregateAbility.Startup))]


namespace HGV.Tarrasque.AggregateAbility
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IAggregateAbilityService, AggregateAbilityService>();
        }
    }
}
