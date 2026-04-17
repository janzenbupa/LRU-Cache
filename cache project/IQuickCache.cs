using System;

namespace Cache
{
    public interface IQuickCache<TKey, TValue> : IDisposable
    {
        IEnumerable<TKey> Keys { get; }
        IEnumerable<TValue> Values { get; }
        bool TryGet(TKey key, out TValue value);
        void Put(TKey key, TValue value, QuickCacheEntryOptions? options);
    }
}
