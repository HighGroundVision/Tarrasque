using HGV.Daedalus;
using HGV.Tarrasque.Api.Models;
using HGV.Tarrasque.Api.Services;
using HGV.Tarrasque.Common;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Polly.Registry;
using System;
using System.Collections.Generic;

[assembly: FunctionsStartup(typeof(HGV.Tarrasque.Api.Startup))]

namespace HGV.Tarrasque.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IAsyncCacheProvider, MemoryCacheProvider>();
            builder.Services.AddSingleton<IReadOnlyPolicyRegistry<string>, PolicyRegistry>((sp) => CacheRegistry.Create(sp));

            builder.Services.AddSingleton<ISteamKeyProvider, SteamKeyProvider>();
            builder.Services.AddSingleton<IDotaApiClient, DotaApiClient>();
            builder.Services.AddSingleton<IHeroService, HeroService>();
            builder.Services.AddSingleton<IPlayerService, PlayerService>();
        }
    }
}

