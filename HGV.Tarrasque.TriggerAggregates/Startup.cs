using HGV.Tarrasque.AggregatesTrigger.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.AggregatesTrigger.Startup))]

namespace HGV.Tarrasque.AggregatesTrigger
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ITriggerAggregatesService, TriggerAggregatesService>();
        }
    }
}
