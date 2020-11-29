﻿using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lib.cache.disk
{
    public class DiskCache : IExtendedDistributedCache
    {
        private string dir;
        private ILogger logger;
        private const string cacheVersion = "v2";

        public DiskCache(string dir, ILogger logger)
        {
            this.dir = dir;
            this.logger = logger;
        }

        public void ClearAll()
        {
            Directory.Delete(this.dir, true);
        }

        public byte[] Get(string key)
        {
            logger.LogDebug($"get key {key} from cache");
            if (!this.Exists(key).Result)
            {
                return null;
            }

            if (this.Expired(key).Result)
            {
                return null;
            }

            return this.GetFromFile(key).Result;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            logger.LogDebug($"get async key {key} from cache");
            if (!await this.Exists(key, token))
            {
                return null;
            }

            if (await this.Expired(key, token))
            {
                return null;
            }

            return await this.GetFromFile(key, token);
        }


        public void Refresh(string key)
        {
            logger.LogDebug($"refresh key {key} from cache");
            if (!this.Exists(key).Result)
            {
                return;
            }

            this.ResetFileTimestamp(key).Wait();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            logger.LogDebug($"refresh async key {key} from cache");
            if (!await this.Exists(key))
            {
                return;
            }

            await this.ResetFileTimestamp(key, token);
        }

        public void Remove(string key)
        {
            logger.LogDebug($"remove key {key} from cache");
            if (!this.Exists(key).Result)
            {
                return;
            }

            this.RemoveFile(key).Wait();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            logger.LogDebug($"remove async key {key} from cache");
            if (!await this.Exists(key))
            {
                return;
            }

            await this.RemoveFile(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            logger.LogDebug($"set key {key} from cache");
            this.SetFile(key, value, options).Wait();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            logger.LogDebug($"set async key {key} from cache");
            await this.SetFile(key, value, options);
        }

        private async Task SetFile(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Directory.CreateDirectory(this.dir);
            string filePath = Path.Join(this.dir, this.KeyHash(key));
            await File.WriteAllBytesAsync(filePath, value, token);
            File.WriteAllText($"{filePath}.json", JsonConvert.SerializeObject(options));
        }

        private async Task<bool> Expired(string key, CancellationToken token = default)
        {
            string filePath = Path.Join(this.dir, this.KeyHash(key));
            var jsonFile = $"{filePath}.json";
            if (!File.Exists(jsonFile))
            {
                logger.LogDebug($"metadata file for key {key} doesn't exist");
                return true;
            }

            var fileInfo = new FileInfo(filePath);

            string jsonData = await File.ReadAllTextAsync(jsonFile, token);
            var options = JsonConvert.DeserializeObject<DistributedCacheEntryOptions>(jsonData);

            if (options.AbsoluteExpiration != null && options.AbsoluteExpiration < DateTime.Now)
            {
                return true;
            }

            if (options.AbsoluteExpirationRelativeToNow != null && fileInfo.LastWriteTime + options.AbsoluteExpirationRelativeToNow < DateTime.Now)
            {
                return true;
            }

            if (options.SlidingExpiration != null && fileInfo.LastAccessTime + options.SlidingExpiration < DateTime.Now)
            {
                return true;
            }

            return false;
        }

        private async Task<bool> Exists(string key, CancellationToken token = default)
        {
            string filePath = Path.Join(this.dir, this.KeyHash(key));
            return await Task.Run(() => File.Exists(filePath), token);
        }

        private async Task<byte[]> GetFromFile(string key, CancellationToken token = default)
        {
            string filePath = Path.Join(this.dir, this.KeyHash(key));
            return await File.ReadAllBytesAsync(filePath, token);
        }

        private async Task ResetFileTimestamp(string key, CancellationToken token = default)
        {
            string filePath = Path.Join(this.dir, this.KeyHash(key));
            var fileInfo = new FileInfo(filePath);
            await Task.Run(() => fileInfo.LastAccessTime = DateTime.Now);
        }

        private async Task RemoveFile(string key, CancellationToken token = default)
        {
            string filePath = Path.Join(this.dir, this.KeyHash(key));
            if (!File.Exists(filePath))
            {
                logger.LogDebug($"not removing file for key {key} since it doesn't exist");
                return;
            }

            await Task.Run(() => File.Delete(filePath));
        }

        private string KeyHash(string key)
        {
            var filename = $"{Escape(key)}.{cacheVersion}";
            return filename;
        }

        static string Escape(string input)
        {
            StringBuilder builder = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                if (Path.GetInvalidPathChars().Contains(input[i]) || Path.GetInvalidFileNameChars().Contains(input[i]) || input[i] == '%')
                {
                    builder.Append(Uri.HexEscape(input[i]));
                }
                else
                {
                    builder.Append(input[i]);
                }
            }
            return builder.ToString();
        }

        static string Unescape(string input)
        {
            StringBuilder builder = new StringBuilder(input.Length);
            int index = 0;
            while (index < input.Length)
            {
                builder.Append(Uri.HexUnescape(input, ref index));
            }
            return builder.ToString();
        }

        public List<string> List()
        {
            var cacheKeys = Directory.GetFiles(this.dir, $"*.{cacheVersion}").Select(ck =>Unescape(ck.Remove(ck.Length - cacheVersion.Length - 1))).ToList();
            var result = new List<string>();

            // TODO: only list items that are not expired
            foreach (var cacheKey in cacheKeys)
            {
                if (this.Expired(cacheKey).Result)
                {
                    continue;
                }

                result.Add(cacheKey);
            }

            return result;
        }
    }
}
