using lib.cache.disk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Reflection;

namespace lib.token_getters
{
    public class TokenGetters : ITokenGetters
    {
        public TokenGetter Amazon {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public TokenGetter Ebay
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public TokenGetter EbayAccess
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public TokenGetter Mercari
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public TokenGetter Poshmark
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public TokenGetter Google
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        private Dictionary<string, TokenGetter> TokenGetterMap;
        private DiskCache TokenCache;
        private readonly ILogger logger;

        public void ClearAllData()
        {
            this.TokenCache.ClearAll();
        }

        public ILogger Logger()
        {
            return this.logger;
        }

        public TokenGetters(ILogger logger, IConfiguration configuration)
        {
            this.TokenCache = new DiskCache("tokens", logger);
            this.TokenGetterMap = new Dictionary<string, TokenGetter>();
            this.logger = logger;

            // Read all token getter settings from configs
            var tokenGetterSettings = new TokenGetterSettings();
            configuration.GetSection("Providers").Bind(tokenGetterSettings);

            foreach (var kv in tokenGetterSettings.TokenGetters)
            {
                var provider = kv.Key;
                var config = kv.Value;

                logger.LogDebug($"registering token provider for {provider}");
                this.TokenGetterMap[provider] = new TokenGetter(
                    this.TokenCache, 
                    logger, 
                    config);
            }
        }
    }
}
