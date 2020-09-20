// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Npgsql;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using System.Reflection;
using System.IO;
using System.Text;
using System.Data;

namespace lib.cache.postgresql
{
    internal class DatabaseOperations : IDatabaseOperations
    {
     
        protected const string GetTableSchemaErrorText =
            "Could not retrieve information of table with schema '{0}' and " +
            "name '{1}'. Make sure you have the table setup and try again. " +
            "Connection string: {2}";

        public DatabaseOperations(
            string connectionString, string schemaName, string tableName, bool createInfrastructure, ISystemClock systemClock)
        {
            ConnectionString = connectionString;
            SchemaName = schemaName;
            TableName = tableName;
            SystemClock = systemClock;
			if (createInfrastructure)
			{
                CreateDatabaseIfNotExist();
                CreateSchemaIfNotExist();
                CreateTableIfNotExist();
            }
        }

        protected string ConnectionString { get; }

        protected string SchemaName { get; }

        protected string TableName { get; }

        protected ISystemClock SystemClock { get; }

        private string ReadScript(string scriptName)
        {
            var assembly = Assembly.Load("lib");
            var resourceStream = assembly.GetManifestResourceStream($"lib.cache.postgresql.scripts.{scriptName}");
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
               return reader.ReadToEnd();
            }
        }
        /// <summary>
        /// Replaces the schema and table names for the ones in config file
        /// </summary>
        /// <returns>The text</returns>
        private string FormatName(string text)
        {
            return text
                    .Replace("[schemaName]", SchemaName)
                    .Replace("[tableName]", TableName);
        }

        // Checks if the database that is going to be built exists or not 
        private bool HasDatabase(string databaseName)
        {
            bool hasDatabase = false;
            NpgsqlConnectionStringBuilder csb = new NpgsqlConnectionStringBuilder(ConnectionString);
            csb.Database = string.Empty;

            using (var cn = new NpgsqlConnection(csb.ToString()))
            {
                cn.Open();

                NpgsqlCommand cmd = new NpgsqlCommand(
                    cmdText: "SELECT datname FROM pg_database",
                    connection: cn);

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        if (dr["datname"].Equals(databaseName))
                        {
                            hasDatabase = true;
                            break;
                        }
                    }
                }
                cn.Close();
            }
            return hasDatabase;
        }

        // If the database doesn't exist, create it
        private void CreateDatabaseIfNotExist()
        {
            NpgsqlConnectionStringBuilder csb = new NpgsqlConnectionStringBuilder(this.ConnectionString);
            var dbname = csb.Database;

            if (this.HasDatabase(dbname) == false)
            {
                csb.Database = string.Empty;
                // Open a connection with the "ConnectionString" options taken from the JSON file
                NpgsqlConnection conn = new NpgsqlConnection(csb.ToString());
                conn.Open();

                // Define a query
                NpgsqlCommand cmd = new NpgsqlCommand("CREATE DATABASE " + dbname, conn);

                // Execute a query
                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        // Create the schemas if they don't exist
        private void CreateSchemaIfNotExist()
        {
            using (var cn = new NpgsqlConnection(this.ConnectionString))
            {
                cn.Open();
                try
                {
                    NpgsqlCommand cmd = new NpgsqlCommand(
                        cmdText: "CREATE SCHEMA IF NOT EXISTS " + SchemaName,
                        connection: cn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
                cn.Close();
            }
        }

        // Create the tables and functions if they don't exist
        private void CreateTableIfNotExist()
        {

            var sql = (
             table: ReadScript("Create_Table_DistCache.sql"),
             funcDateDiff: ReadScript("Create_Function_DateDiff.sql"),
             funcDeleteCacheItem: ReadScript("Create_Function_DeleteCacheItemFormat.sql"),
             funcDeleteExpired: ReadScript("Create_Function_DeleteExpiredCacheItemsFormat.sql"),
             funcGetCacheItem: ReadScript("Create_Function_GetCacheItemFormat.sql"),
             funcSetCache: ReadScript("Create_Function_SetCache.sql"),
             funcUpdateCache: ReadScript("Create_Function_UpdateCacheItemFormat.sql")
             );

            StringBuilder sb = new StringBuilder();
            sb.Append(FormatName(sql.table));
            sb.Append(FormatName(sql.funcDateDiff));
            sb.Append(FormatName(sql.funcGetCacheItem));
            sb.Append(FormatName(sql.funcSetCache));
            sb.Append(FormatName(sql.funcUpdateCache));
            sb.Append(FormatName(sql.funcDeleteCacheItem));
            sb.Append(FormatName(sql.funcDeleteExpired));

            using (var cn = new NpgsqlConnection(this.ConnectionString))
            {
                cn.Open();
                using (var transaction = cn.BeginTransaction())
                {
                    try
                    {
                        NpgsqlCommand cmd = new NpgsqlCommand(
                            cmdText: sb.ToString(),
                            connection: cn,
                            transaction: transaction);
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                        transaction.Rollback();
                    }
                }
                cn.Close();
            }

        }

        public void DeleteCacheItem(string key)
        {
            using (var connection = new NpgsqlConnection(this.ConnectionString))
            {
                var command = new NpgsqlCommand($"{SchemaName}.{Functions.Names.DeleteCacheItemFormat}", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key);

                connection.Open();

                command.ExecuteNonQuery();
            }
        }

        public async Task DeleteCacheItemAsync(string key)
        {
            using (var connection = new NpgsqlConnection(this.ConnectionString))
            {
                var command = new NpgsqlCommand($"{SchemaName}.{Functions.Names.DeleteCacheItemFormat}", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key);

                await connection.OpenAsync();

                await command.ExecuteNonQueryAsync();
            }
        }

        public virtual byte[] GetCacheItem(string key)
        {
            return GetCacheItem(key, includeValue: true);
        }

        public virtual async Task<byte[]> GetCacheItemAsync(string key)
        {
            return await GetCacheItemAsync(key, includeValue: true);
        }

        public void RefreshCacheItem(string key)
        {
            GetCacheItem(key, includeValue: false);
        }

        public async Task RefreshCacheItemAsync(string key)
        {
            await GetCacheItemAsync(key, includeValue: false);
        }

        public virtual void DeleteExpiredCacheItems()
        {
            var utcNow = SystemClock.UtcNow;

            using (var connection = new NpgsqlConnection(this.ConnectionString))
            {
                var command = new NpgsqlCommand($"{SchemaName}.{Functions.Names.DeleteExpiredCacheItemsFormat}", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                connection.Open();

                var effectedRowCount = command.ExecuteNonQuery();
            }
        }

        public virtual void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using (var connection = new NpgsqlConnection(this.ConnectionString))
            {
                var upsertCommand = new NpgsqlCommand($"{SchemaName}.{Functions.Names.SetCache}", connection);
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)                  
                    .AddCacheItemId(key)
                    .AddCacheItemValue(value)
                    .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                    .AddAbsoluteExpiration(absoluteExpiration)
                    .AddParamWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);  

                connection.Open();

                try
                {
                    upsertCommand.ExecuteNonQuery();
                }
                catch (PostgresException ex)
                {
                    if (IsDuplicateKeyException(ex))
                    {
                        // There is a possibility that multiple requests can try to add the same item to the cache, in
                        // which case we receive a 'duplicate key' exception on the primary key column.
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public virtual async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using (var connection = new NpgsqlConnection(this.ConnectionString))
            {
                var upsertCommand = new NpgsqlCommand($"{SchemaName}.{Functions.Names.SetCache}", connection);
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key)
                    .AddCacheItemValue(value)
                    .AddSlidingExpirationInSeconds(options.SlidingExpiration)
                    .AddAbsoluteExpiration(absoluteExpiration)
                    .AddParamWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                await connection.OpenAsync();

                try
                {
                    await upsertCommand.ExecuteNonQueryAsync();
                }
                catch (PostgresException ex)
                {
                    if (IsDuplicateKeyException(ex))
                    {
                        // There is a possibility that multiple requests can try to add the same item to the cache, in
                        // which case we receive a 'duplicate key' exception on the primary key column.
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        protected virtual byte[] GetCacheItem(string key, bool includeValue)
        {
            var utcNow = SystemClock.UtcNow;          

            byte[] value = null;
            TimeSpan? slidingExpiration = null;
            DateTimeOffset? absoluteExpiration = null;
            DateTimeOffset expirationTime;
            using (var connection = new NpgsqlConnection(this.ConnectionString))
            {
                var command = new NpgsqlCommand($"{SchemaName}.{Functions.Names.UpdateCacheItemFormat}", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters
                    .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                    .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                    .AddCacheItemId(key)
                    .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                connection.Open();
                command.ExecuteNonQuery();

                if (includeValue)
                {
                    command = new NpgsqlCommand($"{SchemaName}.{Functions.Names.GetCacheItemFormat}", connection);
					command.CommandType = CommandType.StoredProcedure;
                    command.Parameters
                        .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                        .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                        .AddCacheItemId(key)
                        .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                    var reader = command.ExecuteReader(
                        CommandBehavior.SequentialAccess | CommandBehavior.SingleRow | CommandBehavior.SingleResult);

                    if (reader.Read())
                    {
                        var id = reader.GetFieldValue<string>(Columns.Indexes.CacheItemIdIndex);

                        if (includeValue)
                        {
                            value = reader.GetFieldValue<byte[]>(Columns.Indexes.CacheItemValueIndex);
                        }

                        expirationTime = reader.GetFieldValue<DateTimeOffset>(Columns.Indexes.ExpiresAtTimeIndex);

                        if (!reader.IsDBNull(Columns.Indexes.SlidingExpirationInSecondsIndex))
                        {
                            slidingExpiration = TimeSpan.FromSeconds(
                                reader.GetFieldValue<long>(Columns.Indexes.SlidingExpirationInSecondsIndex));
                        }

                        if (!reader.IsDBNull(Columns.Indexes.AbsoluteExpirationIndex))
                        {
                            absoluteExpiration = reader.GetFieldValue<DateTimeOffset>(
                                Columns.Indexes.AbsoluteExpirationIndex);
                        }
                       
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return value;
        }

        protected virtual async Task<byte[]> GetCacheItemAsync(string key, bool includeValue)
        {
            var utcNow = SystemClock.UtcNow;

            byte[] value = null;
            TimeSpan? slidingExpiration = null;
            DateTimeOffset? absoluteExpiration = null;
            DateTimeOffset expirationTime;
            using (var connection = new NpgsqlConnection(this.ConnectionString))
            {
                var command = new NpgsqlCommand($"{SchemaName}.{Functions.Names.UpdateCacheItemFormat}", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters
                   .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                   .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                   .AddCacheItemId(key)
                   .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                if (includeValue)
                {
                    command = new NpgsqlCommand($"{SchemaName}.{Functions.Names.GetCacheItemFormat}", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters
                        .AddParamWithValue("SchemaName", NpgsqlTypes.NpgsqlDbType.Text, SchemaName)
                        .AddParamWithValue("TableName", NpgsqlTypes.NpgsqlDbType.Text, TableName)
                        .AddCacheItemId(key)
                        .AddWithValue("UtcNow", NpgsqlTypes.NpgsqlDbType.TimestampTz, utcNow);


                    var reader = await command.ExecuteReaderAsync(
                        CommandBehavior.SequentialAccess | CommandBehavior.SingleRow | CommandBehavior.SingleResult);

                    if (await reader.ReadAsync())
                    {
                        var id = await reader.GetFieldValueAsync<string>(Columns.Indexes.CacheItemIdIndex);

                        if (includeValue)
                        {
                            value = await reader.GetFieldValueAsync<byte[]>(Columns.Indexes.CacheItemValueIndex);
                        }

                        expirationTime = await reader.GetFieldValueAsync<DateTimeOffset>(
                            Columns.Indexes.ExpiresAtTimeIndex);

                        if (!await reader.IsDBNullAsync(Columns.Indexes.SlidingExpirationInSecondsIndex))
                        {
                            slidingExpiration = TimeSpan.FromSeconds(
                                await reader.GetFieldValueAsync<long>(Columns.Indexes.SlidingExpirationInSecondsIndex));
                        }

                        if (!await reader.IsDBNullAsync(Columns.Indexes.AbsoluteExpirationIndex))
                        {
                            absoluteExpiration = await reader.GetFieldValueAsync<DateTimeOffset>(
                                Columns.Indexes.AbsoluteExpirationIndex);
                        }
                       
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return value;
        }

        protected bool IsDuplicateKeyException(PostgresException ex)
        {
            return ex.SqlState == "23505";
        }

        protected DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset utcNow, DistributedCacheEntryOptions options)
        {
            // calculate absolute expiration
            DateTimeOffset? absoluteExpiration = null;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value <= utcNow)
                {
                    throw new InvalidOperationException("The absolute expiration value must be in the future.");
                }

                absoluteExpiration = options.AbsoluteExpiration.Value;
            }
            return absoluteExpiration;
        }

        protected void ValidateOptions(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
        {
            if (!slidingExpiration.HasValue && !absoluteExpiration.HasValue)
            {
                throw new InvalidOperationException("Either absolute or sliding expiration needs " +
                    "to be provided.");
            }
        }
    }
}