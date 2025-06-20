namespace Cache.Library.Core
{
    public interface ICache
    {
        bool AddItem(string key, string identifier, List<Dictionary<string, object>> data, bool fill, int? HoursToLive, out string? message, bool expirable = true, bool keepCached = false);
        List<Dictionary<string, object>>? GetItems(string key, out string? message);
        Dictionary<string, object>? GetItem(string key, string identifier, out string? message);
        List<Dictionary<string, object>> GetValues();
        bool SetItemStale(string key);
        bool SetItemExpirability(string key, bool expirable);
        bool SetItemKeepCached(string key, bool keepCached);
        bool SetCapacity(long capacity, bool forced);
        void EvictStaleOrExpired();
        void AdjustValues();
        void LogMetrics();
    }
}
