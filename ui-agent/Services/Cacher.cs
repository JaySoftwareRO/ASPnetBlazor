using lib.cache;
using lib.cache.bifrost.interceptor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace ui_agent.Services
{
    public static class Cacher
    {
        public static IServiceCollection AddCacher(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<ICacher, lib.cache.bifrost.interceptor.Cacher>((container) =>
            {
                var logger = container.GetRequiredService<ILogger<Startup>>();
                var configuration = container.GetRequiredService<IConfiguration>();
                var cache = container.GetRequiredService<IExtendedDistributedCache>();

                return new lib.cache.bifrost.interceptor.Cacher(logger, configuration, cache);
            });
        }
    }
}