using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Http;
// using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Tarrasque.Collection.Startup))]

namespace Tarrasque.Collection
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IMyService, MyService>();

            // builder.Services.AddSingleton<ILoggerProvider, MyLoggerProvider>();
        }
    }
}
