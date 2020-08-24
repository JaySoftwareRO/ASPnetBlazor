using lib.cache.postgresql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bifrost
{
    public interface ICaches
    {
        IDistributedCache Get(string name);
        void Set(string name, IDistributedCache cache);
    }

    public class Caches : ICaches
    {
        private Dictionary<string, IDistributedCache> caches;
        public Caches()
        {
            this.caches = new Dictionary<string, IDistributedCache>();
        }

        public IDistributedCache Get(string name)
        {
            return this.caches[name];
        }

        public void Set(string name, IDistributedCache cache)
        {
            this.caches[name] = cache;
        }
    }

    public class CacheConfig
    {
        public string ConnectionString { get; set; }
        public string Schema { get; set; }
        public string Table { get; set; }
    }

    public class DatabaseSettings
    {
        public Dictionary<string, CacheConfig> Caches { get; set; }
    }

    public static class CachesServices
    {
        public static IServiceCollection AddCaches(this IServiceCollection services, IConfiguration configuration)
        {
            var cachesSettings = new DatabaseSettings();
            configuration.GetSection("Data:Database").Bind(cachesSettings);
            
            var caches = new Caches();

            foreach (var kv in cachesSettings.Caches)
            {
                var schemaName = kv.Value.Schema;
                var tableName = kv.Value.Table;
                var createInfrastructure = true;

                var cache = new PostgreSqlCache(new PostgreSqlCacheOptions()
                {
                    ConnectionString = kv.Value.ConnectionString,
                    SchemaName = schemaName,
                    TableName = tableName,
                    CreateInfrastructure = createInfrastructure,
                });

                caches.Set(kv.Key, cache);
            }

            return services.AddSingleton<ICaches>(caches);
        }
    }
}
