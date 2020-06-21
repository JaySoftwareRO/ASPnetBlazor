using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serialization.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lib.cache
{
    public class DiskCache : IDistributedCache
    {
        private string dir;
        private ILogger logger;

        public DiskCache(string dir, ILogger logger)
        {
            this.dir = dir;
            this.logger = logger;
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
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashValue = sha256.ComputeHash(ASCIIEncoding.ASCII.GetBytes(key));
                return SimpleBase.Base58.Bitcoin.Encode(hashValue);
            }
        } 
    }
}
