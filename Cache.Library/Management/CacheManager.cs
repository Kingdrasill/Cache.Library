using Cache.Library.AdjusterPolicies;
using Cache.Library.Configuration;
using Cache.Library.Core;
using Cache.Library.Core.Models;
using Cache.Library.Diagnostics;
using Cache.Library.Policies;
using Cache.Library.Providers;

namespace Cache.Library.Management
{
    public class CacheManager : ICache
    {
        private readonly object _lock = new object();

        private readonly ICacheProvider _provider;
        private readonly IEvictionPolicy _policy;
        private readonly ICacheItemAdjusterPolicy _adjuster;
        private readonly CacheMetrics _metrics;
        private CacheOptions _options;

        public CacheManager(CacheOptions options)
        {
            _options = options;

            _provider = new DefaultCacheProvider(options.Capacity);
            _policy = CreatePolicy(options.EvictionPolicy);
            _adjuster = CreateAdjuster(options.PolicyAdjuster);
            _metrics = new CacheMetrics();
        }

        private IEvictionPolicy CreatePolicy(string policyName)
        {
            return policyName.ToLower() switch
            {
                "lfru" => new LfruEvictionPolicy(),
                _ => new NoEvictionPolicy()
            };
        }

        private ICacheItemAdjusterPolicy CreateAdjuster(string adjusterName)
        {
            return adjusterName.ToLower() switch
            {
                _ => new DefaultAdjusterPolicy()
            };
        }

        public bool AddItem(string key, string identifier, List<Dictionary<string, object>> data, bool fill, int? HoursToLive, out string? message, bool expirable = true, bool keepCached = false)
        {
            lock (_lock)
            {
                Dictionary<string, Dictionary<string, object>> values = new();

                foreach (var item in data)
                {
                    if (!item.ContainsKey(identifier))
                    {
                        message = "Algum dado passado não possui seu identificador!";
                        return false;
                    }

                    var id = item[identifier].ToString();
                    values[id!] = item;
                }

                var hours = HoursToLive ?? 1;
                var novoItem = new CacheItem(key, values, hours, expirable, keepCached);

                var result = _provider.AddItem(key, novoItem, out message);

                if (!result)
                {
                    if (message == "NotEnoughCapacity") 
                    {
                        message = "Os dados passados são maiores que a cache!";
                        return false;
                    } 
                    else if (message == "NotEnoughSpace" && !fill)
                    {
                        do
                        {
                            var keyRemove = _policy.SelectItemToEvict(_provider.Cache, novoItem.KeepCached, out message);
                            if (keyRemove is null)
                                return false;

                            _provider.RemoveItem(keyRemove);
                            _policy.OnitemRemoved(keyRemove);
                        }
                        while (!_provider.AddItem(key, novoItem, out message));
                    } 
                    else
                    {
                        message = "Erro inesperado aconteceu!";
                        return false;
                    }
                }
                
                _policy.OnitemAdded(key);
                return result;
            }
        }

        public Dictionary<string, object>? GetItem(string key, string identifier, out string? message)
        {
            lock (_lock)
            {
                var item = _provider.GetItem(key, identifier);

                if (item is null)
                {
                    message = "Este dado não está na cache!";
                    return null;
                }

                if (_provider.ExpiredOrStale(key))
                {
                    message = "Este dado está inválido na cache!";
                    return null;
                }

                _policy.OnItemAcessed(key);
                message = null;
                return item;
            }
        }

        public List<Dictionary<string, object>>? GetItems(string key, out string? message)
        {
            lock (_lock)
            {
                var item = _provider.Get(key);

                if (item is null)
                {
                    message = "Este conjunto de dados não está na cache!";
                    return null;
                }

                if (_provider.ExpiredOrStale(key))
                {
                    message = "Este dado está inválido na cache!";
                    return null;
                }

                _policy.OnItemAcessed(key);
                var listItems = new List<Dictionary<string, object>>();

                foreach (var itemInCache in item.Values.Values)
                {
                    listItems.Add(itemInCache);
                }

                message = null;
                return listItems;
            }
        }

        public List<Dictionary<string, object>> GetValues()
        {
            lock (_lock)
            {
                List<Dictionary<string, object>> values = new();

                foreach (var item in _provider.Cache)
                {
                    var key = item.Key;
                    var value = item.Value;
                    var freq = _policy.GetFreq(key);

                    values.Add(new Dictionary<string, object>{
                        { "Key", key },
                        { "Frequency", freq },
                        { "HoursToLive", value.HoursToLive },
                        { "Expirable", value.Expirable },
                        { "KeepCached", value.KeepCached }
                    });
                }

                return values;
            }
        }

        public bool SetItemStale(string key)
        {
            lock (_lock)
            {
                if (!_provider.Includes(key))
                {
                    return false;
                }
                return _provider.SetItemStale(key);
            }
        }

        public bool SetItemExpirability(string key, bool expirable)
        {
            lock (_lock)
            {
                if (!_provider.Includes(key))
                {
                    return false;   
                }
                return _provider.SetItemExpirability(key, expirable);
            }
        }

        public bool SetItemKeepCached(string key, bool keepCached)
        {
            lock (_lock)
            {
                if (!_provider.Includes(key))
                {
                    return false;
                }
                return _provider.SetItemKeepCached(key, keepCached);
            }
        }

        public bool SetCapacity(long capacity, bool forced)
        {
            if (capacity < 0) return false;
            lock (_lock)
            { 
                while (!_provider.SetCapacity(capacity))
                {
                    if (!forced)
                        return false;

                    var key = _policy.SelectItemToEvict(_provider.Cache, forced, out var message);
                    if (key is null)
                        return false;

                    _provider.RemoveItem(key!);
                    _policy.OnitemRemoved(key!);
                }
                _options.SetCapacity(capacity);
                _metrics.LogCapacity(_options);
                return true;
            }
        }

        public void EvictStaleOrExpired()
        {
            lock (_lock)
            {
                var keys = _provider.EvictStaleOrExpired();

                foreach (var key in keys)
                {
                    _policy.OnitemRemoved(key);
                }
            }
        }
        public void AdjustValues()
        {
            lock (_lock)
            {
                foreach (var item in _provider.Cache)
                {
                    var freq = _policy.GetFreq(item.Key);
                    var values = _adjuster.Adjust(freq);
                    _provider.AdjustValues(item.Key, values);
                }
            }
        }

        public void LogMetrics() 
        {

            lock (_lock)
            {
                Dictionary<string, int> freqs = new();
                List<CacheItem> items = new();

                foreach (var item in _provider.Cache)
                {
                    freqs.Add(item.Key, _policy.GetFreq(item.Key));
                    items.Add(item.Value);
                }

                _metrics.LogUsedCache(_options, _provider.CurrentSize, items, freqs);
            }
        }
    }
}
