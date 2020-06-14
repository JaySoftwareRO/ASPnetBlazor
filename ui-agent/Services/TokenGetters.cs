using lib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            return services.AddSingleton<ITokenGetters>(new lib.HardcodedTokenGetters());
        }
    }
}
