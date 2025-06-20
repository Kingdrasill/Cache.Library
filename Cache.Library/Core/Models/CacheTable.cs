namespace Cache.Library.Core.Models
{
    public class CacheTable
    {
        private readonly object _lock = new();
        private Dictionary<string, CacheItem> _cache { get; set; }
        private long _capacity { get; set; }
        private long _usedSize { get; set; }

        public CacheTable(long capacity)
        {
            _cache = new Dictionary<string, CacheItem>();
            _capacity = capacity;
            _usedSize = 0;
        }

        public bool Includes(string key)
        {
            lock (_lock)
            {
                return _cache.ContainsKey(key);
            }
        }

        public bool Includes(string key, string identifier)
        {
            lock (_lock)
            {
                if (_cache.ContainsKey(key))
                {
                    return _cache[key].Values.ContainsKey(identifier);
                }
                return false;
            }
        }

        public bool ExpiredOrStale(string key)
        {
            lock (_lock)
            {
                if (_cache.ContainsKey(key))
                {
                    return (_cache[key].Stale || (_cache[key].Expirable && _cache[key].LastUsed.AddHours(_cache[key].HoursToLive) < DateTime.UtcNow));
                }
                return false;
            }
        }

        public bool TryGet(string key, out CacheItem? item)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var foundItem))
                {
                    foundItem.MarkAsUsed();
                    item = foundItem;
                    return true;
                }
                item = null;
                return false;
            }
        }

        public bool TryGetItem(string key, string identifier, out Dictionary<string, object>? item)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var foundCItem))
                {
                    if (foundCItem.Values.TryGetValue(identifier, out var founditem))
                    {
                        foundCItem.MarkAsUsed();
                        item = founditem;
                        return true;
                    }
                }
                item = null;
                return false;
            }
        }

        public bool AddOrUpdate(string key, CacheItem item, out string? message)
        {
            lock (_lock) 
            {
                if (item.EstimatedSize > _capacity)
                {
                    message = "NotEnoughCapacity";
                    return false;
                }

                if (_usedSize + item.EstimatedSize > _capacity)
                {
                    message = "NotEnoughSpace";
                    return false;
                }

                if (_cache.ContainsKey(key))
                {
                    _usedSize -= _cache[key].EstimatedSize;
                    _cache[key] = item;
                }
                else
                {
                    _cache.Add(key, item);
                }

                _usedSize += item.EstimatedSize;
                _cache[key].MarkAsUsed();

                message = null;
                return true;
            }
        }

        public bool SetCapacity(long capacity)
        {
            lock (_lock)
            {
                if (capacity < _usedSize)
                {
                    return false;
                }

                _capacity = capacity;
                return true;
            }
        }

        public bool SetItemStale(string key)
        {
            lock (_lock) {
                if (_cache.ContainsKey(key))
                {
                    _cache[key].MarkAsStale();
                    return true;
                }
                return false; 
            }
        }

        public bool SetItemExpirability(string key, bool expirable)
        {
            lock (_lock)
            {
                if(_cache.ContainsKey(key))
                {
                    _cache[key].UpdateExpirable(expirable);
                }
                return false;
            }
        }

        public bool SetItemKeepCached(string key, bool keepCached)
        {
            lock (_lock)
            {
                if (_cache.ContainsKey(key))
                {
                    _cache[key].UpdateKeepCached(keepCached);
                }
                return false;
            }
        }

        public List<string> EvictStaleOrExpired()
        {
            var listKey = new List<string>();
            var itemsToRemove = _cache.Values
                                    .Where(i => i.Stale || (i.Expirable && i.LastUsed.AddHours(i.HoursToLive) < DateTime.UtcNow))
                                    .ToList();

            foreach (var item in itemsToRemove)
            {
                Destroy(item.Key);
                listKey.Add(item.Key);
            }

            return listKey;
        }

        public void Destroy(string key)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    _usedSize -= item.EstimatedSize;
                    _cache.Remove(key);
                }
            }
        }

        public void AdjustValues(string key, Dictionary<string, object> values)
        {
            lock (_lock)
            {
                _cache[key].UpdateHoursToLive((int)values["hours"]);
                _cache[key].UpdateExpirable((bool)values["exp"]);
            }
        }

        public Dictionary<string, CacheItem> Cache => _cache;
        public long Capacity => _capacity;
        public long UsedSize => _usedSize;
    }
}
