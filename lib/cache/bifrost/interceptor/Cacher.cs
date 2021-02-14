using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace lib.cache.bifrost.interceptor
{
    public delegate T CacheableCall<T>();
    public delegate bool ValidValue<T>(T value);

    public class Cacher : ICacher
    {
        // new Cacher(IExtendedDistributedCache)
        // var items = cacher["ebay-list"].Run(this.accountID, () => {

        // }
        private readonly IDistributedCache cache;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public Cacher(ILogger logger, IConfiguration configuration, IExtendedDistributedCache cache)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.cache = cache;
        }

        public async Task<T> Run<T>(string id, CacheableCall<T> call, ValidValue<T> isValid)
        {
            if (string.IsNullOrEmpty(id)) {
                logger.LogDebug("id empty when trying to run cachable call");
                throw new ArgumentException("can't use an empty id", id);
            }

            var cachedBytes = await this.cache.GetAsync(id);

            if (cachedBytes != null)
            {
                var cachedValue = JsonConvert.DeserializeObject<T>(ASCIIEncoding.UTF8.GetString(cachedBytes));

                if (isValid(cachedValue))
                {
                    return cachedValue;
                }

                await this.cache.RemoveAsync(id);
            }

            var value = call();
            var jsonValue = ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));

            await this.cache.SetAsync(id, jsonValue,
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(this.configuration.GetValue<int>("Bifrost:Cache:ExpirationTimeSeconds"))
                });

            return value;
        }
    }
}