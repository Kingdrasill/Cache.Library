using Cache.Library.Core;
using Cache.Library.Core.Models;

namespace Cache.Library.Policies
{
    public class LfruEvictionPolicy : IEvictionPolicy
    {
        private readonly object _lock = new();
        private readonly LinkedList<string> _accessOrder = new();
        private readonly Dictionary<string, int> _frequencyCounter = new();

        public void OnItemAcessed(string key)
        {
            lock (_lock)
            {
                if (_frequencyCounter.ContainsKey(key))
                {
                    _frequencyCounter[key] += 1;
                    _accessOrder.Remove(key);
                    _accessOrder.AddLast(key);
                }
            }
        }

        public void OnitemAdded(string key)
        {
            lock (_lock)
            {
                _frequencyCounter[key] = 1;
                _accessOrder.AddLast(key);
            }
        }

        public void OnitemRemoved(string key)
        {
            lock (_lock)
            {
                _frequencyCounter.Remove(key);
                _accessOrder.Remove(key);
            }
        }

        public string? SelectItemToEvict(Dictionary<string, CacheItem> cache, bool forced, out string? message)
        {
            lock (_lock)
            {
                var stales = cache.Values
                                    .Where(c => c.Stale)
                                    .OrderByDescending(c => c.EstimatedSize)
                                    .Select(c => c.Key)
                                    .ToList();
                if (stales.Count > 0)
                {
                    message = null;
                    return stales[0];
                }

                var nonPersistent = cache.Values
                                        .Where(nk => !nk.KeepCached)
                                        .Select(nk => nk.Key)
                                        .ToList();
                if (nonPersistent.Count != 0)
                {
                    var leastUsed = GetLeastFrequent(nonPersistent);
                    if (leastUsed.Count == 1)
                    {
                        message = null;
                        return leastUsed[0];
                    }
                    var oldest = GetOldest(leastUsed);

                    message = null;
                    return oldest;
                }

                if (!forced)
                {
                    message = "Nada pode ser deletado, pois todos os dados da cache são persistentes!";
                    return null;
                }

                var leastUsedAll = GetLeastFrequent(cache.Keys.ToList());
                if (leastUsedAll.Count == 1)
                {
                    message = null;
                    return leastUsedAll[0];
                }
                var oldestAll = GetOldest(leastUsedAll);

                message = null;
                return oldestAll;
            }
        }
        public int GetFreq(string key)
        {
            return _frequencyCounter[key];
        }

        private string? GetOldest(IEnumerable<string> keys)
        {
            return keys.FirstOrDefault(k => _accessOrder.Contains(k));
        }

        private List<string> GetLeastFrequent(IEnumerable<string> keys) 
        {
            var minFreq = keys.Min(k => _frequencyCounter[k]);
            return keys.Where(k => _frequencyCounter[k] == minFreq).ToList();
        }
    }
}
