using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;

namespace lib.cache
{
    public interface IExtendedDistributedCache: IDistributedCache
    {
        public List<string> List();
    }
}
