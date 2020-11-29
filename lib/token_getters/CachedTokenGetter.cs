using lib.cache;
using lib.cache.disk;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace lib.token_getters
{
    public class CachedTokenGetter : ITokenGetter
    {
        private IExtendedDistributedCache cache;
        private string cacheTokenKey;
        private string cacheUserIDKey;
        private int cacheHours = 30;
        private string loginURL;
        private ILogger logger;
        private List<string> scopes;

        public event OnTokenValidationDelegate OnTokenValidation;
        public CachedTokenGetter(IExtendedDistributedCache cache, ILogger logger, string cacheKey, string loginURL, int cacheHours, List<string> scopes)
        {
            this.cache = cache;
            this.cacheTokenKey = string.Format($"{cacheKey}_token");
            this.cacheUserIDKey = string.Format($"{cacheKey}_userid");
            this.cacheHours = cacheHours;
            this.loginURL = loginURL;
            this.logger = logger;
            this.scopes = scopes;
        }

        public async Task Set(string token, string userID)
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
    }
}
