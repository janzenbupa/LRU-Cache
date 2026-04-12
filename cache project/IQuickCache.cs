using System;

namespace Cache
{
    public interface IQuickCache<TKey, TValue> : IDisposable
    {
        IEnumerable<TValue> Get();
        bool TryGet(TKey key, out TValue value);
        void Create(TKey key, TValue value, DateTimeOffset? expiry);
    }
}
