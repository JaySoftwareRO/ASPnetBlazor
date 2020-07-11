using lib.cache.postgresql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bifrost
{
    public static class Caches
    {
        public static IServiceCollection AddCaches(this IServiceCollection services, string connectionString)
        {
            var schemaName = "bifrost_schemas";
            var tableName = "ebay";
            var createInfrastructure = true;

            var cache = new PostgreSqlCache(new PostgreSqlCacheOptions()
            {
                ConnectionString = connectionString,
                SchemaName = schemaName,
                TableName = tableName,
                CreateInfrastructure = createInfrastructure,
            });

            return services.AddSingleton<IDistributedCache>(cache);
        }
    }
}
