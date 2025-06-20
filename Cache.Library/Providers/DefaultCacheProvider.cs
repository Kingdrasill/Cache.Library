using Cache.Library.Core;
using Cache.Library.Core.Models;

namespace Cache.Library.Providers
{
    public class DefaultCacheProvider : ICacheProvider
    {
        private readonly CacheTable _cache;

        public DefaultCacheProvider(long capacity)
        {
            _cache = new CacheTable(capacity);
        }
        
        public bool Includes(string key) => _cache.Includes(key);
        public bool Includes(string key, string identifier) => _cache.Includes(key, identifier);
        public bool ExpiredOrStale(string key) => _cache.ExpiredOrStale(key);
        public CacheItem? Get(string key) => _cache.TryGet(key, out var item) ? item : null;
        public Dictionary<string, object>? GetItem(string key, string identifier) => _cache.TryGetItem(key, identifier, out var item) ? item : null;
        public bool AddItem(string key, CacheItem item, out string? message) => _cache.AddOrUpdate(key, item, out message);
        public void RemoveItem(string key) => _cache.Destroy(key);
        public void AdjustValues(string key, Dictionary<string, object> values) => _cache.AdjustValues(key, values);
        public bool SetItemStale(string key) => _cache.SetItemStale(key);
        public bool SetItemExpirability(string key, bool expirable) => _cache.SetItemExpirability(key, expirable);
        public bool SetItemKeepCached(string key, bool keepCached) => _cache.SetItemKeepCached(key, keepCached);
        public List<string> EvictStaleOrExpired() => _cache.EvictStaleOrExpired();

        public Dictionary<string, CacheItem> Cache => _cache.Cache;
        public long CurrentSize => _cache.UsedSize;
        public long Capacity => _cache.Capacity;

        public bool SetCapacity(long capacity) => _cache.SetCapacity(capacity);
    }
}
