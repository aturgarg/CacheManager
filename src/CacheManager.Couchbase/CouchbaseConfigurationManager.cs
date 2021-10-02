using System;
using System.Collections.Generic;
using CacheManager.Core.Utility;
using Couchbase;

namespace CacheManager.Couchbase
{
    /// <summary>
    /// 
    /// </summary>
    public static class CouchbaseConfigurationManager
    {
        private static Dictionary<string, ClusterOptions> _config = new Dictionary<string, ClusterOptions>();
        private static object _configLock = new object();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationName"></param>
        /// <returns></returns>
        public static ClusterOptions GetConfiguration(string configurationName)
        {
            Guard.NotNullOrWhiteSpace(configurationName, nameof(configurationName));

            if (!_config.ContainsKey(configurationName))
            {
                throw new InvalidOperationException("No configuration added for configuration name " + configurationName);
            }

            return _config[configurationName];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="clusterOptions"></param>
        public static void AddConfiguration(string key, ClusterOptions clusterOptions)
        {
            lock (_configLock)
            {
                Guard.NotNull(clusterOptions, nameof(clusterOptions));
                Guard.NotNullOrWhiteSpace(key, nameof(key));

                if (!_config.ContainsKey(key))
                {
                    _config.Add(key, clusterOptions);
                }
            }
        }
    }
}
