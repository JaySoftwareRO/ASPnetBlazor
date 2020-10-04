﻿using lib.cache.disk;
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
        public int TokenCacheDurationHours { get; set; }
    }

    public class TokenGetterSettings
    {
        public Dictionary<string, TokenGetterConfig> TokenGetters { get; set; }
    }

    public delegate bool OnTokenValidationDelegate(ITokenGetter tokenGetter, string token, ILogger logger);

    public interface ITokenGetter
    {
        event OnTokenValidationDelegate OnTokenValidation;

        Task<string> GetToken();

        Task<string> GetUserID();

        string LoginURL();

        List<string> Scopes();

        Task Set(string token, string userID);
    }

    public interface ITokenGetters
    {
        ITokenGetter Ebay { get; }
        ITokenGetter EbayAccess { get; }
        ITokenGetter Mercari { get; }
        ITokenGetter Poshmark { get; }
        ITokenGetter Amazon { get; }
        ITokenGetter Google { get; }
    }

    public class TokenGetters : ITokenGetters
    {
        public ITokenGetter Amazon {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public ITokenGetter Ebay
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public ITokenGetter EbayAccess
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public ITokenGetter Mercari
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public ITokenGetter Poshmark
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
            }
        }

        public ITokenGetter Google
        {
            get
            {
                return this.TokenGetterMap[MethodBase.GetCurrentMethod().Name.Replace("get_", "")];
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
                    config.TokenCacheDurationHours,
                    config.Scopes);

                switch (provider)
                {
                    case "EbayAccess":
                        this.TokenGetterMap[provider].OnTokenValidation += OnEbayAccessTokenValidation;
                        break;
                    default:
                        break;
                }
            }
        }

        private bool OnEbayAccessTokenValidation(ITokenGetter tokenGetter, string token, ILogger logger)
        {
            // Validate the token
            try
            {
                EbayTokenUtils.UserID(token, logger);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "access token for ebay is not valid");
                return false;
            }

            return true;
        }
    }
}
