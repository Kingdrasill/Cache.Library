using Cache.Library.Configuration;
using Cache.Library.Core.Models;

namespace Cache.Library.Diagnostics
{
    public class CacheMetrics
    {
        public CacheMetrics() { }

        public void LogCapacity(CacheOptions _configuration)
        {
            Console.WriteLine($"Current cache capacity: {_configuration.Capacity}");
        }

        public void LogUsedCache(CacheOptions _configuration, long usedSize, List<CacheItem> items, Dictionary<string, int> frequencies) 
        {
            Console.WriteLine($"Current cache used: {usedSize / 1024 / 1024}/{_configuration.Capacity / 1024 / 1024} MB");

            Console.WriteLine("Cache itens:");

            foreach (var item in items)
                Console.WriteLine($"\t - Chave: {item.Key}, Frequência: {frequencies[item.Key]}, HoursToLive:{item.HoursToLive}, Expirable = {item.Expirable}, KeepCached = {item.KeepCached}");
        }
    }
}
