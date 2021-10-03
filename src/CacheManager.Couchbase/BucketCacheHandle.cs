﻿using System;
using System.Security.Cryptography;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using Couchbase;
using Couchbase.KeyValue;
using Couchbase.Management.Buckets;
using Newtonsoft.Json.Linq;
using static CacheManager.Core.Utility.Guard;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;

namespace CacheManager.Couchbase
{
    /// <summary>
    /// Cache handle implementation based on the couchbase .net client.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class BucketCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private readonly CouchbaseManager _couchbaseManager;
        private readonly IBucketManager _bucketManager;
        private readonly IBucket _bucket;
        private readonly string _bucketName;

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketCacheHandle{TCacheValue}" /> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>        
        /// <param name="loggerFactory">The logger factory.</param>       
        /// <exception cref="System.InvalidOperationException">If <c>configuration.HandleName</c> is not valid.</exception>
        public BucketCacheHandle(
            ICacheManagerConfiguration managerConfiguration,
            CacheHandleConfiguration configuration,
            ILoggerFactory loggerFactory)
            : base(managerConfiguration, configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            Logger = loggerFactory.CreateLogger(this);

            ClusterOptions clusterOptions = CouchbaseConfigurationManager.GetConfiguration(Constants.CouchbaseConfigurationFullKey);

            // we can configure the bucket name by having "<configKey>:<bucketName>" as handle's
            // this should only be used in 100% by app/web.config based configuration
            var nameParts = configuration.Key.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length >= 2)
            {
                _bucketName = nameParts[1];
            }

            _couchbaseManager = new CouchbaseManager(clusterOptions);
            //_bucketManager = _couchbaseManager.GetBucketManagerAsync().WaitAndUnwrapException();
            //_bucket = _couchbaseManager.GetBucketAsync(_bucketName).WaitAndUnwrapException();
            _bucketManager = AsyncContext.Run(() => _couchbaseManager.GetBucketManagerAsync());
            _bucket = AsyncContext.Run(() => _couchbaseManager.GetBucketAsync(_bucketName));
        }

        /// <inheritdoc />
        public override bool IsDistributedCache => true;

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => (int)Stats.GetStatistic(CacheStatsCounterType.Items);

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            if (_bucketManager != null)
            {
                _bucketManager.FlushBucketAsync(_bucketName);
            }
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.NotImplementedException">Not supported in this version.</exception>
        public override void ClearRegion(string region)
        {
            // TODO: not supported?
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            var fullKey = GetKey(key);
            IExistsResult result = AsyncContext.Run(() => _bucket.DefaultCollection().ExistsAsync(fullKey));
            return result.Exists;
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            var fullKey = GetKey(key, region);
            IExistsResult result = AsyncContext.Run(() => _bucket.DefaultCollection().ExistsAsync(fullKey));
            return result.Exists;
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));
            var fullKey = GetKey(item.Key, item.Region);
            bool exists;
            if (!string.IsNullOrWhiteSpace(item.Region))
            {
                exists = Exists(item.Key, item.Region);
            }
            else
            {
                exists = Exists(item.Key);
            }
            IMutationResult result = null;

            if (!exists)
            {
                if (item.ExpirationMode != ExpirationMode.None)
                {
                    var options = new InsertOptions();
                    options.Expiry(item.ExpirationTimeout);
                    result = AsyncContext.Run(() => _bucket.DefaultCollection().InsertAsync(fullKey, item, options));
                }

                result = AsyncContext.Run(() => _bucket.DefaultCollection().InsertAsync(fullKey, item));
            }

            return (result != null && result.Cas > 0 && result.MutationToken != null
                        && result.MutationToken.VBucketUuid > 0);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposeManaged">Indicator if managed resources should be released.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) =>
            GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullkey = GetKey(key, region);
            var result = AsyncContext.Run(() => _bucket.DefaultCollection().GetAsync(fullkey));

            if (result == null)
            {
                return null;
            }

            var cacheItem = result.ContentAs<CacheItem<TCacheValue>>();
            if (cacheItem.Value is JToken)
            {
                var value = cacheItem.Value as JToken;
                cacheItem = cacheItem.WithValue((TCacheValue)value.ToObject(cacheItem.ValueType));
            }

            if (cacheItem.IsExpired)
            {
                return null;
            }

            // extend sliding expiration
            if (cacheItem.ExpirationMode == ExpirationMode.Sliding)
            {
                cacheItem.LastAccessedUtc = DateTime.UtcNow;
                PutInternalPrepared(cacheItem);
            }

            return cacheItem;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            var fullKey = GetKey(item.Key, item.Region);
            IMutationResult result;
            if (item.ExpirationMode == ExpirationMode.Absolute || item.ExpirationMode == ExpirationMode.Sliding)
            {
                var options = new UpsertOptions();
                options.Expiry(item.ExpirationTimeout);
                result = AsyncContext.Run(() => _bucket.DefaultCollection().UpsertAsync(fullKey, item, options));
            }
            else
            {
                result = AsyncContext.Run(() => _bucket.DefaultCollection().UpsertAsync(fullKey, item));
            }
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key, string region)
        {
            var fullKey = GetKey(key, region);

            return AsyncContext.Run(() => _bucket.DefaultCollection().RemoveAsync(fullKey)
                                .ContinueWith((action) =>
                                    { return action.IsCompleted && !action.IsFaulted; })
                                );
        }

        private static string GetSHA256Key(string key)
        {
            using (var sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static string GetKey(string key, string region = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                fullKey = string.Concat(region, ":", key);
            }

            // Memcached still has a 250 character limit
            if (fullKey.Length >= 250)
            {
                return GetSHA256Key(fullKey);
            }

            return fullKey;
        }
    }
}
