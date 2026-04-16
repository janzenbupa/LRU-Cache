using System;

namespace Cache
{
    public class QuickCacheEntryOptions
    {
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public long Size { get; set; } = 1;
    }
}
