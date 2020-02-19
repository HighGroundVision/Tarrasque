using Polly.Registry;
using System;
using System.Collections.Generic;
using Polly;
using Microsoft.Extensions.DependencyInjection;
using HGV.Tarrasque.Api.Models;
using Polly.Caching;

namespace HGV.Tarrasque.Api
{
    public static class CacheRegistry
    {
        public static PolicyRegistry Create(IServiceProvider serviceProvider)
        {
            PolicyRegistry registry = new PolicyRegistry();
            registry.Add("FnHeroesHistory",
                Policy.CacheAsync(
                    serviceProvider.GetRequiredService<IAsyncCacheProvider>().AsyncFor<List<HeroHistory>>(),
                    TimeSpan.FromMinutes(5)
                )
            );
            registry.Add("FnLeaderboardGlobal",
                Policy.CacheAsync(
                    serviceProvider.GetRequiredService<IAsyncCacheProvider>().AsyncFor<List<PlayerModel>>(),
                    TimeSpan.FromMinutes(5)
                )
            );
            registry.Add("FnLeaderboardByRegion",
                Policy.CacheAsync(
                    serviceProvider.GetRequiredService<IAsyncCacheProvider>().AsyncFor<List<PlayerModel>>(),
                    TimeSpan.FromMinutes(5),
                    context => context.OperationKey + context["region"]
                )
            );
            return registry;
        }
    }
}
