namespace Cache.Library.Configuration
{
    public class CacheOptions
    {
        public long Capacity { get; set; } = 128 * 1024 * 1024;
        public string EvictionPolicy { get; set; } = "lfru";
        public string PolicyAdjuster { get; set; } = "default";

        public void SetCapacity(long capacity)
        {
            Capacity = capacity;
        }
    }
}
