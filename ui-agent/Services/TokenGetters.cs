using lib;
using lib.token_getters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

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

            return services.AddSingleton<ITokenGetters, lib.token_getters.TokenGetters>((container) =>
            {
                var logger = container.GetRequiredService<ILogger<Startup>>();
                var configuration = container.GetRequiredService<IConfiguration>();

                return new lib.token_getters.TokenGetters(logger, configuration);
            });
        }
    }
}
