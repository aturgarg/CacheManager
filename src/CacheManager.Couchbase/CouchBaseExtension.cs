using Couchbase.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Couchbase;
using System;

namespace CacheManager.Couchbase
{
    /// <summary>
    /// 
    /// </summary>
    public static class CouchBaseExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection WithCouchbaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection("CouchbaseClusterOptions");
            var clusterOptions = new ClusterOptions();
            settings.Bind(clusterOptions);
            services.Configure<ClusterOptions>((a) => { a = clusterOptions; });

            //services.AddCouchbase(settings);
            //services.AddCouchbaseBucket<INamedBucketProvider>("");

            //services.AddTransient<CouchbaseManager>();

            CouchbaseConfigurationManager.AddConfiguration("CouchbaseClusterOptions", clusterOptions);

            return services;
        }
    }
}
