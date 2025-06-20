using Cache.Library.Core;

namespace Cache.Library.AdjusterPolicies
{
    public class DefaultAdjusterPolicy : ICacheItemAdjusterPolicy
    {
        public Dictionary<string, object> Adjust(int frequency)
        {
            var dict = new Dictionary<string, object>();

            if (frequency <= 10)
            {
                dict["hours"] = 1;
                dict["exp"] = true;
            }
            else if (frequency <= 50)
            {
                dict["hours"] = (int)(frequency / 10);
                dict["exp"] = true;
            }
            else if (frequency < 100)
            {
                dict["hours"] = 10;
                dict["exp"] = true;
            }
            else
            {
                dict["hours"] = 20;
                dict["exp"] = false;
            }

            return dict;
        }
    }
}
