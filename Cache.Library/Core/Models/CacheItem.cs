namespace Cache.Library.Core.Models
{
    public class CacheItem
    {
        public CacheItem() { }

        public string Key { get; }
        public Dictionary<string, Dictionary<string, object>> Values { get; }

        private readonly object _stateLock = new();

        public int HoursToLive { get; private set; }
        public DateTime LastUsed { get; private set; }
        public bool Expirable { get; private set; }
        public bool KeepCached { get; private set; }
        public bool Stale { get; private set; }
        public long EstimatedSize 
        {
            get
            {
                long size = 0;
                foreach (var item in Values.Values) 
                {
                    size += item.Keys.Sum(i => i.Length * sizeof(char));
                    size += item.Values.Sum(i => EstimateValueSize(i));
                }
                return size;
            }
        }

        public CacheItem(string key, Dictionary<string, Dictionary<string, object>> values, int hoursToLive, bool expirable, bool keepCached)
        {
            Key = key;
            Values = values;
            LastUsed = DateTime.UtcNow;
            HoursToLive = hoursToLive;
            Expirable = expirable;
            KeepCached = keepCached;
            Stale = false;
        }

        public void MarkAsUsed()
        {
            lock (_stateLock)
            {
                LastUsed = DateTime.UtcNow;
            }
        }

        public void MarkAsStale()
        {
            lock (_stateLock)
            {
                Stale = true;
            }
        }

        public void UpdateHoursToLive(int hoursToLive)
        {
            lock (_stateLock)
            {
                HoursToLive = hoursToLive;
            }
        }

        public void UpdateExpirable(bool expirable)
        {
            lock (_stateLock)
            {
                Expirable = expirable;
            }
        }

        public void UpdateKeepCached(bool keepCached)
        {
            lock (_stateLock)
            {
                KeepCached = keepCached;
            }
        }

        private static long EstimateValueSize(object value)
        {
            if (value is null) return 0;
            if (value is string s) return s.Length * sizeof(char);
            if (value is int or float or double or decimal) return sizeof(double);
            return 16;
        }

        public override string ToString()
        {
            return $"Key: {Key}, Size: {EstimatedSize} bytes, LastUsed: {LastUsed}, TTL: {HoursToLive}h, Stale: {Stale}";
        }
    }
}
