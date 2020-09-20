using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using lib.bifrost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace bifrost
{
    public class CacheService : Cache.CacheBase
    {
        private readonly ILogger<CacheService> logger;
        private readonly ICaches caches;

        public CacheService(ILogger<CacheService> logger, ICaches caches)
        {
            this.logger = logger;
            this.caches = caches;
        }

        public override async Task<SetReply> Set(SetRequest request, ServerCallContext context)
        {
            var cache = this.caches.Get(request.Cache);
           
            var cacheOptions = new DistributedCacheEntryOptions();
            if (request.CacheOptions != null)
            {
                if (!string.IsNullOrWhiteSpace(request.CacheOptions.AbsoluteExpiration))
                {
                    cacheOptions.AbsoluteExpiration = DateTimeOffset.Parse(request.CacheOptions.AbsoluteExpiration);
                }

                if (!string.IsNullOrWhiteSpace(request.CacheOptions.AbsoluteExpirationRelativeToNow))
                {
                    cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.Parse(request.CacheOptions.AbsoluteExpirationRelativeToNow);
                }

                if (!string.IsNullOrWhiteSpace(request.CacheOptions.SlidingExpiration))
                {
                    cacheOptions.SlidingExpiration = TimeSpan.Parse(request.CacheOptions.SlidingExpiration);
                }
            }
            var key = GenerateKey(request.Key, context);
            await cache.SetAsync(key, request.Value.ToByteArray(), cacheOptions);
            return new SetReply();
        }

        public override async Task<GetReply> Get(GetRequest request, ServerCallContext context)
        {
            var cache = this.caches.Get(request.Cache);
            var key = GenerateKey(request.Key, context);

            var value = await cache.GetAsync(key);
            return new GetReply()
            {
                Value = value == null ? ByteString.Empty : ByteString.CopyFrom(value),
                HasValue = value != null,
            };
        }

        public override async Task<RefreshReply> Refresh(RefreshRequest request, ServerCallContext context)
        {
            var cache = this.caches.Get(request.Cache);
            var key = GenerateKey(request.Key, context);

            await cache.RefreshAsync(key);
            return new RefreshReply();
        }

        public override async Task<RemoveReply> Remove(RemoveRequest request, ServerCallContext context)
        {
            var cache = this.caches.Get(request.Cache);
            var key = GenerateKey(request.Key, context);

            await cache.RemoveAsync(key);
            return new RemoveReply();
        }
        // Key created from the request key and the Google account ID. 
        public string GenerateKey(string key, ServerCallContext context)
        {
            var googleAccountId = context.RequestHeaders.Get("googleprofileid");
            return $"{key}_{googleAccountId.Value}";
        }
    }
}
