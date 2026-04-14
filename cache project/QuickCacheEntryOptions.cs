using System;

namespace Cache
{
    public class QuickCacheEntryOptions
    {
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
    }
}
