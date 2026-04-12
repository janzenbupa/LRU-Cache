namespace Cache
{
    public class QuickCache<TKey, TValue> : IQuickCache<TKey, TValue>
    {
        private bool _disposed;
        public void Dispose()
        {
            ThrowIfDisposed();
            cache.Clear();
            cacheNodes.Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(QuickCache<TKey, TValue>));
        }

        private class itemEntry
        {
            public TKey key;
            public TValue value;
            public DateTimeOffset? expiry;
        }


        private LinkedList<itemEntry> cacheNodes;
        private Dictionary<TKey, LinkedListNode<itemEntry>> cache;

        private int capacity;
        private readonly object _lock;

        public QuickCache()
        {
            this.capacity = 3000;
            cache = new Dictionary<TKey, LinkedListNode<itemEntry>>(this.capacity);
            cacheNodes = new LinkedList<itemEntry>();
            _lock = new object();
        }
        public IEnumerable<TValue> Get()
        {
            ThrowIfDisposed();
            return cache.Values.Select(s => s.Value.value).ToList();
        }

        public bool TryGet(TKey key, out TValue value)
        {
            ThrowIfDisposed();
            if (cache.TryGetValue(key, out var node))
            {
                if (node.Value.expiry is DateTimeOffset expiry && expiry > DateTimeOffset.UtcNow)
                {
                    value = node.Value.value;
                    cacheNodes.Remove(node);
                    cacheNodes.AddLast(node);
                    return true;
                }
                else
                {
                    cache.Remove(node.Value.key);
                    cacheNodes.Remove(node);
                }
            }
            value = default!;
            return false;
        }

        public void Create(TKey key, TValue value, DateTimeOffset? expiry)
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                if (cache.ContainsKey(key))
                {
                    cacheNodes.Remove(cache[key]);
                }
                else if (cache.Count > capacity)
                {
                    var node = cacheNodes.First;
                    cache.Remove(node!.Value.key);
                }

                var newNode = new LinkedListNode<itemEntry>(new itemEntry { key = key, value = value, expiry = expiry });
                cache[key] = newNode;
                cacheNodes.AddLast(newNode);
            }
        }
    }
}
