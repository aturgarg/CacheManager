using CacheManager.Couchbase;
using Couchbase;
using Microsoft.Extensions.Configuration;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the Couchbase cache handle.
    /// </summary>
    public static class CouchbaseConfigurationBuilderExtensions
    {
        /// <summary>     
        ///
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">The key which has to match with the cache handle name.</param>
        /// <param name="configuration"></param>
        /// <returns>
        /// The configuration builder.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If <paramref name="configurationKey" /> or <paramref name="configuration" /> is null.</exception>
        public static ConfigurationBuilderCachePart WithCouchbaseConfiguration(this ConfigurationBuilderCachePart part,
            string configurationKey, IConfiguration configuration)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
                       
            var clusterOptions = new ClusterOptions();
            var settings = configuration.GetSection(configurationKey);
            settings.Bind(clusterOptions);
            CouchbaseConfigurationManager.AddConfiguration(configurationKey, clusterOptions);

            //services.Configure<ClusterOptions>((action) => { action = clusterOptions; });
           
            return part;
        }
       
        /// <summary>
        /// Adds an already configured <see cref="ICluster" /> for the given key. Use this in case you want to use the <paramref name="cluster" /> outside of CacheManager, too
        /// and you want to share this instance.     
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">The configuration key.</param>
        /// <param name="cluster">The <see cref="ICluster" />.</param>
        /// <returns>
        /// The configuration builder.
        /// <exception cref="System.ArgumentNullException">If <paramref name="configurationKey" /> or <paramref name="cluster" /> is null.</exception>
        /// </returns>
        public static ConfigurationBuilderCachePart WithCouchbaseCluster(this ConfigurationBuilderCachePart part,
            string configurationKey, ICluster cluster)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            NotNull(cluster, nameof(cluster));

            CouchbaseManager.AddCluster(configurationKey, cluster);
            return part;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="couchbaseConfigurationKey">The configuration identifier.</param>
        /// <param name="bucketName">The name of the Couchbase bucket which should be used by the cache handle.</param>
        /// <param name="bucketPassword">The bucket password.</param>
        /// <param name="isBackplaneSource">Set this to <c>true</c> if this cache handle should be the source of the backplane. This setting will be ignored if no backplane is configured.</param>

        /// <returns></returns>
        public static ConfigurationBuilderCacheHandlePart WithCouchbaseCacheHandle(
            this ConfigurationBuilderCachePart part,
            string couchbaseConfigurationKey,
            string bucketName,
            string bucketPassword,
            bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));
            NotNullOrWhiteSpace(bucketName, nameof(bucketName));

            return part.WithHandle(typeof(BucketCacheHandle<>), couchbaseConfigurationKey, isBackplaneSource);
        }

    }
}
