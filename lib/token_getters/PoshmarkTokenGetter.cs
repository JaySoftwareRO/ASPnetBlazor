using lib.cache.disk;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.token_getters
{
    public class PoshmarkHardcodedTokenGetter : ITokenGetter
    {
        string token;
        string user_id;
        DiskCache diskCache;
        string key;

        public async System.Threading.Tasks.Task SetAsync(string token, string user_id, DiskCache diskCache, string key)
        {
            this.token = token;
            this.diskCache = diskCache;
            this.key = key;

            var cachedToken = await this.diskCache.GetAsync(key);
            if (cachedToken == null)
            {
                await SetCachedTokenAsync(key);
            } 
            else
            {
                GetCachedTokenAsync();
            }

            this.user_id = user_id;
        }

        private string GetCachedTokenAsync()
        {
            return token = ASCIIEncoding.UTF8.GetString(diskCache.Get(key));
        }

        public void Set(string token)
        {
            this.token = token;
        }

        public string Get()
        {
            return token;
        }

        public string GetUserID()
        {
            return user_id;
        }

        public string LoginURL()
        {
            return "https://poshmark.com/login";
        }

        private async System.Threading.Tasks.Task SetCachedTokenAsync(string tokenKey)
        {
            await this.diskCache.SetAsync(tokenKey,
                ASCIIEncoding.UTF8.GetBytes(token),
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                });
        }
    }
}
