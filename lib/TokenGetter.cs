using lib.cache.disk;
using lib.token_getters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace lib
{
    public interface ITokenGetter
    {
        Task<string> GetToken();

        Task<string> GetUserID();

        string LoginURL();

        Task Set(string token, string userID);
    }

    public interface ITokenGetters
    {
        ITokenGetter EBayTokenGetter();
        ITokenGetter MercariTokenGetter();
        ITokenGetter PoshmarkTokenGetter();
        ITokenGetter AmazonTokenGetter();

    }

    public class TokenGetters : ITokenGetters
    {
        private ITokenGetter amazon;
        private ITokenGetter ebay;
        private ITokenGetter mercari;
        private ITokenGetter poshmark;

        public TokenGetters(ILogger logger, IConfiguration configuration)
        {
            IDistributedCache tokenCache = new DiskCache("tokens", logger);
           
            this.amazon = new CachedTokenGetter(tokenCache, logger, configuration["Providers:Amazon:CacheKey"], configuration["Providers:Amazon:LoginURL"], configuration.GetValue<int>("Providers:Amazon:TokenCacheDurationDays"));
            this.ebay = new CachedTokenGetter(tokenCache, logger, configuration["Providers:EBay:CacheKey"], configuration["Providers:EBay:LoginURL"], configuration.GetValue<int>("Providers:EBay:TokenCacheDurationDays"));
            this.mercari = new CachedTokenGetter(tokenCache, logger, configuration["Providers:Mercari:CacheKey"], configuration["Providers:Mercari:LoginURL"], configuration.GetValue<int>("Providers:Mercari:TokenCacheDurationDays"));
            this.poshmark = new CachedTokenGetter(tokenCache, logger, configuration["Providers:Poshmark:CacheKey"], configuration["Providers:Poshmark:LoginURL"], configuration.GetValue<int>("Providers:Poshmark:TokenCacheDurationDays"));
        }

        public ITokenGetter AmazonTokenGetter()
        {
            return this.amazon;
        }

        public ITokenGetter EBayTokenGetter()
        {
            return this.ebay;
        }

        public ITokenGetter MercariTokenGetter()
        {
            return this.mercari;
        }

        public ITokenGetter PoshmarkTokenGetter()
        {
            return this.poshmark;
        }
    }
}
