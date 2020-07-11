using bifrost;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static bifrost.Cache;

namespace lib.cache.bifrost
{
    public class BifrostCache : IDistributedCache
    {
        private string dir;
        private CacheClient client;
        private ILogger logger;

        public BifrostCache(string address, ILogger logger)
        {
            var httpHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            // TODO: the credentials should not be hardcoded
            var httpCredentials = "NmQ4NDYxMzkyM2VhNDVkN2E0MTQ5OGZlNzI2ZGE2Nzc6NTBlNWU1MmY2YmMxNDllMzhjMWY2MTQ5MDliZWZjNTg=";
            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                metadata.Add("Authorization", $"{httpCredentials}");
                return Task.CompletedTask;
            });

            // TODO: this unsecure setting should be configurable
            var channel = GrpcChannel.ForAddress(
                address,
                new GrpcChannelOptions {
                    HttpHandler = httpHandler,
                    Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
                }
            );

            this.client = new CacheClient(channel);
            this.logger = logger;
        }

        public byte[] Get(string key)
        {
            logger.LogDebug($"get key {key} from bifrost cache");

            var result = this.client.Get(
                new GetRequest
                {
                    Key = key
                });

            if (!result.HasValue)
            {
                return null;
            }

            return result.Value.ToByteArray();
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            logger.LogDebug($"get key {key} from bifrost cache");

            var result = await this.client.GetAsync(
                new GetRequest
                {
                    Key = key
                });

            if (!result.HasValue)
            {
                return null;
            }

            return result.Value.ToByteArray();
        }


        public void Refresh(string key)
        {
            logger.LogDebug($"refresh key {key} from bifrost cache");
            this.client.Refresh(new RefreshRequest{});
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            logger.LogDebug($"refresh async key {key} from bifrost cache");
            await this.client.RefreshAsync(new RefreshRequest { });
        }

        public void Remove(string key)
        {
            logger.LogDebug($"remove key {key} from bifrost cache");
            this.client.Remove(new RemoveRequest { });
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            logger.LogDebug($"remove async key {key} from cache");
            await this.client.RemoveAsync(new RemoveRequest { });
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            logger.LogDebug($"set key {key} from bifrost cache");
            this.client.Set(new SetRequest { 
                Key = key,
                Value = ByteString.CopyFrom(value),
                CacheOptions =  new CacheOptions()
                {
                    AbsoluteExpiration = options.AbsoluteExpiration == null ? "" : options.AbsoluteExpiration.ToString(),
                    AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow == null ? "" : options.AbsoluteExpirationRelativeToNow.ToString(),
                    SlidingExpiration = options.SlidingExpiration == null ? "" : options.SlidingExpiration.ToString(),
                }
            });
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            logger.LogDebug($"set key {key} from bifrost cache");
            await this.client.SetAsync(new SetRequest
            {
                Key = key,
                Value = ByteString.CopyFrom(value),
                CacheOptions = new CacheOptions()
                {
                    AbsoluteExpiration = options.AbsoluteExpiration == null ? "" : options.AbsoluteExpiration.ToString(),
                    AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow == null ? "" : options.AbsoluteExpirationRelativeToNow.ToString(),
                    SlidingExpiration = options.SlidingExpiration == null ? "" : options.SlidingExpiration.ToString(),
                }
            });
        }
    }
}
