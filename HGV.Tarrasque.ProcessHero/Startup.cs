using HGV.Daedalus;
using HGV.Tarrasque.ProcessHero.Services;
using HGV.Tarrasque.Common;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.ProcessHero.Startup))]


namespace HGV.Tarrasque.ProcessHero
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IProcessHeroService, ProcessHeroService>();
        }
    }
}
