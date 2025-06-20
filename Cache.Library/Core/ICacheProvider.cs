using Cache.Library.Core.Models;

namespace Cache.Library.Core
{
    public interface ICacheProvider
    {
        bool Includes(string key);
        bool Includes(string key, string identifier);
        bool ExpiredOrStale(string key);
        CacheItem? Get(string key);
        Dictionary<string, object>? GetItem(string key, string identifier);
        bool AddItem(string key, CacheItem item, out string? message);
        void RemoveItem(string key);
        void AdjustValues(string key, Dictionary<string, object> values);
        bool SetItemStale(string key);
        bool SetItemExpirability(string key, bool expirable);
        bool SetItemKeepCached(string key, bool keepCached);
        List<string> EvictStaleOrExpired();

        Dictionary<string, CacheItem> Cache { get; }
        long CurrentSize { get; }
        long Capacity { get; }

        bool SetCapacity(long capacity);
    }
}
