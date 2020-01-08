using HGV.Tarrasque.TriggerAggregates.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.TriggerAggregates.Startup))]

namespace HGV.Tarrasque.TriggerAggregates
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ITriggerAggregatesService, TriggerAggregatesService>();
        }
    }
}
