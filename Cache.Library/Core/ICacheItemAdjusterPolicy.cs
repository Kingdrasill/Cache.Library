namespace Cache.Library.Core
{
    public interface ICacheItemAdjusterPolicy
    {
        public Dictionary<string, object> Adjust(int frequency);
    }
}
