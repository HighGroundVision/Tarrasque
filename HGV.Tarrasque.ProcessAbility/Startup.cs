using HGV.Tarrasque.ProcessAbility.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.ProcessAbility.Startup))]

namespace HGV.Tarrasque.ProcessAbility
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IProcessAbilitiesService, ProcessAbilitiesService>();
        }
    }
}
