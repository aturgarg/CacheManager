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
        //private static ConcurrentDictionary<string, ClientConfiguration> _configurations = new ConcurrentDictionary<string, ClientConfiguration>();
        private static ConcurrentDictionary<string, ICluster> _clusters = new ConcurrentDictionary<string, ICluster>();
        //private readonly string _configurationName;
        //private readonly string _bucketName;
        //private readonly string _bucketPassword;
        //private readonly INamedBucketProvider _bucketProvider;
        private readonly ClusterOptions _clusterOptions;
        //private readonly ICluster _cluster;

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
        public CouchbaseManager(ClusterOptions clusterOptions)
        {
            _clusterOptions = clusterOptions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<ICluster> GetClusterAsync()
        {
            try
            {
                if (!_clusters.ContainsKey(_clusterOptions.ConnectionString))
                {
                    ICluster cluster = await Cluster.ConnectAsync(_clusterOptions);
                    _clusters.TryAdd(_clusterOptions.ConnectionString, cluster);
                }

                return _clusters[_clusterOptions.ConnectionString];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public async Task<IBucket> GetBucketAsync(string bucketName)
        {
            try
            {
                ICluster cluster = await GetClusterAsync();

                return await cluster.BucketAsync(bucketName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IBucketManager> GetBucketManagerAsync()
        {
            try
            {
                ICluster cluster = await GetClusterAsync();

                return cluster.Buckets;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

    }
}
