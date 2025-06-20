using Cache.Library.Core;
using Cache.Library.Core.Models;

namespace Cache.Library.Policies
{
    public class NoEvictionPolicy : IEvictionPolicy
    {
        public void OnItemAcessed(string key) { }

        public void OnitemAdded(string key) { }

        public void OnitemRemoved(string key) { }

        public string? SelectItemToEvict(Dictionary<string, CacheItem> cache, bool forced, out string? message)
        {
            if (forced)
            {
                var firstKey = cache.Keys.FirstOrDefault();
                if (firstKey != null)
                {
                    message = "Eviction forçada — sem política definida, removendo o primeiro item encontrado.";
                    return firstKey;
                }
            }

            message = "Nenhum item pode ser removido — política de cache sem eviction configurada.";
            return null;
        }

        public int GetFreq(string key)
        {
            return 1;
        }
    }
}
