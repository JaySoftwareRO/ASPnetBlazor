using lib;
using lib.token_getters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ui_agent.Services
{
    public static class TokenGetters
    {
        public static IServiceCollection AddTokenGetters(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<ITokenGetters, lib.TokenGetters>((container) =>
            {
                var logger = container.GetRequiredService<ILogger<Startup>>();
                var configuration = container.GetRequiredService<IConfiguration>();

                return new lib.TokenGetters(logger, configuration);
            });
        }
    }
}
