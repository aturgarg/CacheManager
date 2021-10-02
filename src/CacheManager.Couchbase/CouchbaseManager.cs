using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core.Configuration.Server;
using Couchbase.Management.Buckets;
using Couchbase.KeyValue;
using CacheManager.Core.Utility;
using Couchbase.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CacheManager.Couchbase
{
    /// <summary>
    /// 
    /// </summary>
    public class CouchbaseManager
    {
        /// <summary>
        /// 
        /// </summary>
        public const string DefaultBucketName = "default";

        private static object _configLock = new object();
        private static ConcurrentDictionary<string, ClientConfiguration> _configurations = new ConcurrentDictionary<string, ClientConfiguration>();
        private static ConcurrentDictionary<string, ICluster> _clusters = new ConcurrentDictionary<string, ICluster>();
        //private readonly string _configurationName;
        //private readonly string _bucketName;
        //private readonly string _bucketPassword;
        //private readonly INamedBucketProvider _bucketProvider;
        private readonly IOptions<ClusterOptions> _clusterOptions;
        //private readonly ICluster _cluster;

        /// <summary>
        /// 
        /// </summary>
        public ICluster ClusterInstance { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public IBucket CacheBucket { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ICouchbaseCollection CacheCollection { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clusterOptions"></param>
        /// <returns></returns>
        public CouchbaseManager(IOptions<ClusterOptions> clusterOptions)
        {
            _clusterOptions = clusterOptions;           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<ICluster> GetClusterAsync()
        {
            if (ClusterInstance == null)
            {              
                ClusterInstance = await Cluster.ConnectAsync(_clusterOptions.Value);
            }

            //_clusters.TryAdd(configurationKey, cluster);

            return ClusterInstance;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public async Task<IBucket> GetBucketAsync(string bucketName)
        {

            return await ClusterInstance.BucketAsync(bucketName);

            //return await _bucketProvider.GetBucketAsync();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IBucketManager> GetBucketManagerAsync()
        {
            await GetClusterAsync();
            return ClusterInstance.Buckets;           

        }

    }
}
