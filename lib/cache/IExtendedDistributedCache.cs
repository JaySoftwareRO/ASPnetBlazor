using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.cache
{
    public interface IExtendedDistributedCache: IDistributedCache
    {
        public List<string> List();
    }
}
