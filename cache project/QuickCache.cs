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
            public long size;
            public DateTimeOffset? absExpiry;
            public TimeSpan? slidingExpiry;
            public DateTimeOffset lastAccessed;
        }


        private LinkedList<itemEntry> cacheNodes;
        private Dictionary<TKey, LinkedListNode<itemEntry>> cache;

        public IEnumerable<TKey> Keys
        {
            get
            {
                lock (_lock)
                {
                    return cache.Keys.ToList();
                }
            }
        }

        public long Size
        {
            get
            {
                lock (_lock)
                {
                    return currentSize;
                }
            }
        }

        public long MaxSize { get { return _maxSize; } }

        private int capacity;
        private readonly object _lock;
        private readonly Timer _timer;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(30);

        private long currentSize;
        private readonly long _maxSize;

        public QuickCache(long maxSize, int capacity = 7000)
        {
            if (maxSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize));
            this.capacity = capacity;
            cache = new Dictionary<TKey, LinkedListNode<itemEntry>>(this.capacity);
            cacheNodes = new LinkedList<itemEntry>();

            _lock = new object();
            _timer = new Timer(_ => CleanUp(), null, _cleanupInterval, _cleanupInterval);
            _maxSize = maxSize;
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
                if (options == null)
                    options = new QuickCacheEntryOptions();
                if (options.Size <= 0)
                    throw new ArgumentOutOfRangeException(nameof(options.Size));
                if (options.Size > _maxSize)
                    throw new InsufficientMemoryException("Size of object cannot exceed max size of cache.");

                if (cache.ContainsKey(key))
                {
                    currentSize -= cache[key].Value.size;
                    cacheNodes.Remove(cache[key]);
                }

                var now = DateTimeOffset.UtcNow;

                var newNode = new LinkedListNode<itemEntry>(new itemEntry
                {
                    key = key,
                    value = value,
                    size = options.Size,
                    lastAccessed = now,
                    absExpiry = options?.AbsoluteExpirationRelativeToNow != null ?
                        now.Add(options.AbsoluteExpirationRelativeToNow.Value) : null,
                    slidingExpiry = options?.SlidingExpiration
                });
                cache[key] = newNode;
                cacheNodes.AddLast(newNode);
                currentSize += newNode.Value.size;

                while (currentSize > _maxSize || cache.Count > capacity)
                {
                    var node = cacheNodes.First;
                    cache.Remove(node!.Value.key);
                    currentSize -= node.Value.size;
                    cacheNodes.RemoveFirst();
                }
            }
        }

        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (!cache.TryGetValue(key, out var node))
                {
                    return false;
                }

                currentSize -= node.Value.size;
                cacheNodes.Remove(node);
                cache.Remove(key);
                return true;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                cache.Clear();
                cacheNodes.Clear();
                currentSize = 0;
            }
        }
    }
}
