using lib.cache;
using lib.cache.postgresql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bifrost
{
    public interface ICaches
    {
        IExtendedDistributedCache Get(string name);
        void Set(string name, IExtendedDistributedCache cache);
    }

    public class Caches : ICaches
    {
        private Dictionary<string, IExtendedDistributedCache> caches;
        public Caches()
        {
            this.caches = new Dictionary<string, IExtendedDistributedCache>();
        }

        public IExtendedDistributedCache Get(string name)
        {
            return this.caches[name];
        }

        public void Set(string name, IExtendedDistributedCache cache)
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

    public class CheckDuplicates
    {
        public bool SchemaDuplicates(string schemaName)
        {
            bool HasDuplicate = false;

            List<string> schemas = new List<string>(); // To store the schemas
            List<string> tempSchemas = new List<string>(); // To iterate 

            tempSchemas.Add("dummySchema");
            foreach(var schema in tempSchemas)
            {
                if (schema == schemaName)
                {
                    HasDuplicate = true;
                    break;
                }
                schemas.Add(schemaName);
            }
            tempSchemas = schemas;
            return HasDuplicate;
        }
    }

    public static class CachesServices
    {
        public static IServiceCollection AddCaches(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            var cachesSettings = new DatabaseSettings();
            configuration.GetSection("Data:Database").Bind(cachesSettings);

            var caches = new Caches();
            var duplicate = new CheckDuplicates();

            foreach (var kv in cachesSettings.Caches)
            {
                var schemaName = kv.Value.Schema; 
                var tableName = kv.Value.Table;
                var createInfrastructure = true;

                if (duplicate.SchemaDuplicates(schemaName) == false)
                {
                    var cache = new PostgreSqlCache(new PostgreSqlCacheOptions()
                    {
                        ConnectionString = kv.Value.ConnectionString,
                        SchemaName = schemaName,
                        TableName = tableName,
                        CreateInfrastructure = createInfrastructure,
                    }, logger);
                    caches.Set(kv.Key, cache);
                }
            }

            return services.AddSingleton<ICaches>(caches);
        }
    }
}
