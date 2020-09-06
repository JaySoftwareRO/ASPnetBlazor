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
        private IDistributedCache cache;
        private string cacheTokenKey;
        private string cacheUserIDKey;
        private int cacheDays = 30;
        private string loginURL;
        private ILogger logger;

        public CachedTokenGetter(IDistributedCache cache, ILogger logger, string cacheKey, string loginURL, int cacheDays)
        {
            this.cache = cache;
            this.cacheTokenKey = string.Format($"{cacheKey}_token");
            this.cacheUserIDKey = string.Format($"{cacheKey}_userid");
            this.cacheDays = cacheDays;
            this.loginURL = loginURL;
        }

        public async Task Set(string token, string userID)
        {
            await this.cache.SetStringAsync(
                this.cacheTokenKey,
                token,
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(this.cacheDays)
                });

            await this.cache.SetStringAsync(
                this.cacheUserIDKey,
                userID,
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(this.cacheDays)
                });
        }

        public async Task<string> GetToken()
        {
            return await this.cache.GetStringAsync(this.cacheTokenKey);
        }

        public async Task<string> GetUserID()
        {
            return await this.cache.GetStringAsync(this.cacheUserIDKey);
        }

        public string LoginURL()
        {
            return this.loginURL;
        }
    }
}
