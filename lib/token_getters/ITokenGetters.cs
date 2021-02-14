using lib.cache.disk;
using lib.token_getters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace lib.token_getters
{
    public delegate bool OnTokenValidationDelegate(TokenGetter tokenGetter, string token, ILogger logger);

    public interface ITokenGetters
    {
        TokenGetter Ebay { get; }
        TokenGetter EbayAccess { get; }
        TokenGetter Mercari { get; }
        TokenGetter Poshmark { get; }
        TokenGetter Amazon { get; }
        TokenGetter Google { get; }

        void ClearAllData();
        ILogger Logger();
    }
}
