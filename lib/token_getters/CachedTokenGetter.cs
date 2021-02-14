using lib.cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lib.token_getters
{
    public class TokenGetter
    {
        private IExtendedDistributedCache cache;
        private string cacheTokenKey;
        private string cacheUserIDKey;
        private string cacheUsernameKey;
        private int cacheHours = 30;
        private string loginURL;
        private ILogger logger;
        private List<string> scopes;

        public event OnTokenValidationDelegate OnTokenValidation;
        public TokenGetter(IExtendedDistributedCache cache, ILogger logger, TokenGetterConfig config)
        {
            this.cache = cache;
            this.cacheTokenKey = string.Format($"{config.CacheKey}_token");
            this.cacheUserIDKey = string.Format($"{config.CacheKey}_userid");
            this.cacheUsernameKey = string.Format($"{config.CacheKey}_username");
            this.cacheHours = config.TokenCacheDurationHours;
            this.loginURL = config.LoginURL;
            this.logger = logger;
            this.scopes = config.Scopes;
            this.Config = config;
        }

        public TokenGetterConfig Config { get; set; }

        public async Task Set(string token, string userID, string username)
        {
            this.logger.LogDebug($"recording token and user in {this.cacheTokenKey} cache");
            await this.cache.SetStringAsync(
                this.cacheTokenKey,
                token,
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromHours(this.cacheHours)
                });

            await this.cache.SetStringAsync(
                this.cacheUserIDKey,
                userID,
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromHours(this.cacheHours)
                });

            await this.cache.SetStringAsync(
                this.cacheUsernameKey,
                username,
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromHours(this.cacheHours)
                });
        }

        public async Task<string> GetToken()
        {
            this.logger.LogDebug($"getting token from {this.cacheTokenKey} cache");
            var token = await this.cache.GetStringAsync(this.cacheTokenKey);

            if (this.OnTokenValidation != null && !this.OnTokenValidation(this, token, logger))
            {
                return null;
            }

            return token;
        }

        public async Task<string> GetUserID()
        {
            this.logger.LogDebug($"getting user in {this.cacheTokenKey} cache");
            return await this.cache.GetStringAsync(this.cacheUserIDKey);
        }

        public List<string> Scopes()
        {
            return this.scopes;
        }

        public string LoginURL()
        {
            return this.loginURL;
        }

        public async Task<string> GetUsername()
        {
            this.logger.LogDebug($"getting username in {this.cacheUsernameKey} cache");
            return await this.cache.GetStringAsync(this.cacheUsernameKey);
        }
    }
}
