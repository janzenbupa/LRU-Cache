namespace Cache
{
    public class QuickCache<TKey, TValue> : IQuickCache<TKey, TValue> where TKey : notnull
    {
        private bool _disposed;
        public void Dispose()
        {
            ThrowIfDisposed();
            cache.Clear();
            cacheNodes.Clear();
            _disposed = true;
            _timer.Dispose();
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(QuickCache<TKey, TValue>));
        }

        private void CleanUp()
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                var node = cacheNodes.First;

                while (node != null)
                {
                    var prev = node.Previous;
                    var item = node.Value;

                    bool exp = (item.absExpiry is DateTimeOffset abs && abs < now) ||
                        (item.slidingExpiry is TimeSpan s && item.lastAccessed.Add(s) < now);

                    if (exp)
                    {
                        cache.Remove(item.key);
                        cacheNodes.Remove(node);
                    }

                    node = prev;
                }
            }
        }

        private class itemEntry
        {
            public required TKey key;
            public required TValue value;
            public DateTimeOffset? absExpiry;
            public TimeSpan? slidingExpiry;
            public DateTimeOffset lastAccessed;
        }


        private LinkedList<itemEntry> cacheNodes;
        private Dictionary<TKey, LinkedListNode<itemEntry>> cache;

        public IEnumerable<TKey> Keys { get; private set; }

        private int capacity;
        private readonly object _lock;
        private readonly Timer _timer;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(30);

        public QuickCache(int capacity = 3000)
        {
            this.capacity = capacity;
            cache = new Dictionary<TKey, LinkedListNode<itemEntry>>(this.capacity);
            cacheNodes = new LinkedList<itemEntry>();
            Keys = new HashSet<TKey>();

            _lock = new object();
            _timer = new Timer(_ => CleanUp(), null, _cleanupInterval, _cleanupInterval);
        }
        public IEnumerable<TValue> Get()
        {
            ThrowIfDisposed();
            foreach (var node in cache.Values)
                yield return node.Value.value;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            ThrowIfDisposed();
            if (cache.TryGetValue(key, out var node))
            {
                var now = DateTimeOffset.UtcNow;

                if (node.Value.absExpiry is DateTimeOffset expiry && expiry < DateTimeOffset.UtcNow)
                {
                    cache.Remove(key);
                    cacheNodes.Remove(node);
                    value = default!;
                    return false;
                }

                if (node.Value.slidingExpiry is TimeSpan s && node.Value.lastAccessed.Add(s) < now)
                {
                    cache.Remove(key);
                    cacheNodes.Remove(node);
                    value = default!;
                    return false;
                }

                node.Value.lastAccessed = now;
                cacheNodes.Remove(node);
                cacheNodes.AddLast(node);

                value = node.Value.value;
                return true;
            }
            value = default!;
            return false;
        }

        public void Create(TKey key, TValue value, QuickCacheEntryOptions? options = null)
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                if (cache.ContainsKey(key))
                {
                    cacheNodes.Remove(cache[key]);
                }

                var now = DateTimeOffset.UtcNow;

                var newNode = new LinkedListNode<itemEntry>(new itemEntry
                {
                    key = key,
                    value = value,
                    lastAccessed = now,
                    absExpiry = options?.AbsoluteExpirationRelativeToNow != null ?
                        now.Add(options.AbsoluteExpirationRelativeToNow.Value) : null,
                    slidingExpiry = options?.SlidingExpiration
                });
                cache[key] = newNode;
                cacheNodes.AddLast(newNode);

                if (cache.Count > capacity)
                {
                    var node = cacheNodes.First;
                    cache.Remove(node!.Value.key);
                    cacheNodes.RemoveFirst();
                }
            }
        }
    }
}
