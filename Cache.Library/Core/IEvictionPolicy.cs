using Cache.Library.Core.Models;

namespace Cache.Library.Core
{
    public interface IEvictionPolicy
    {
        void OnItemAcessed(string key);
        void OnitemAdded(string key);
        void OnitemRemoved(string key);
        string? SelectItemToEvict(Dictionary<string, CacheItem> cache, bool forced, out string? message);
        int GetFreq(string key);
    }
}
