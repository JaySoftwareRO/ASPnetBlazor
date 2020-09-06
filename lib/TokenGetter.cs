using lib.cache.disk;
using lib.token_getters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace lib
{
    public class TokenGetterConfig
    {
        public string LoginURL { get; set; }
        public string CacheKey { get; set; }
        public List<string> Scopes { get; set; }
        public int TokenCacheDurationDays { get; set; }
    }

    public class TokenGetterSettings
    {
        public Dictionary<string, TokenGetterConfig> TokenGetters { get; set; }
    }
    public interface ITokenGetter
    {
        Task<string> GetToken();

        Task<string> GetUserID();

        string LoginURL();

        List<string> Scopes();

        Task Set(string token, string userID);
    }

    public interface ITokenGetters
    {
        ITokenGetter EBayTokenGetter();
        ITokenGetter EbayAccessTokenGetter();
        ITokenGetter MercariTokenGetter();
        ITokenGetter PoshmarkTokenGetter();
        ITokenGetter AmazonTokenGetter();
    }

    public class TokenGetters : ITokenGetters
    {
        private ITokenGetter Amazon {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name];
            }
        }

        private ITokenGetter Ebay
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name];
            }
        }


        private ITokenGetter EbayAccess
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name];
            }
        }

        private ITokenGetter Mercari
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name];
            }
        }

        private ITokenGetter Poshmark
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name];
            }
        }

        private Dictionary<string, ITokenGetter> TokenGetterMap;

        public TokenGetters(ILogger logger, IConfiguration configuration)
        {
            IDistributedCache tokenCache = new DiskCache("tokens", logger);
            this.TokenGetterMap = new Dictionary<string, ITokenGetter>();

            // Read all token getter settings from configs
            var tokenGetterSettings = new TokenGetterSettings();
            configuration.GetSection("Providers").Bind(tokenGetterSettings);

            foreach (var kv in tokenGetterSettings.TokenGetters)
            {
                var provider = kv.Key;
                var config = kv.Value;

                logger.LogDebug($"registering token provider for {provider}");
                this.TokenGetterMap[provider] = new CachedTokenGetter(
                    tokenCache, 
                    logger, 
                    config.CacheKey, 
                    config.LoginURL,
                    config.TokenCacheDurationDays,
                    config.Scopes);
            }
        }

        public ITokenGetter AmazonTokenGetter()
        {
            return this.Amazon;
        }

        public ITokenGetter EBayTokenGetter()
        {
            return this.Ebay;
        }

        public ITokenGetter MercariTokenGetter()
        {
            return this.Mercari;
        }

        public ITokenGetter PoshmarkTokenGetter()
        {
            return this.Poshmark;
        }

        public ITokenGetter EbayAccessTokenGetter()
        {
            return this.EbayAccess;
        }
    }
}
