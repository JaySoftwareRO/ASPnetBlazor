using lib.cache.disk;
using lib.token_getters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace lib.token_getters
{
    public class TokenGetterSettings
    {
        public Dictionary<string, TokenGetterConfig> TokenGetters { get; set; }
    }

    public class TokenGetterConfig
    {
        public string LoginURL { get; set; }
        public string CacheKey { get; set; }
        public List<string> Scopes { get; set; }
        public int TokenCacheDurationHours { get; set; }
        public int EbayLiveCallLimit { get; set; }
        public string EbayMode { get; set; }
    }
}
