using System.Collections.Generic;

namespace LRUCache
{
    public class LRUCache
    {
        private int capacity;
        private LinkedList<(int key, int value)> timer;
        private Dictionary<int, LinkedListNode<(int key, int value)>> cache;

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
            this.timer = new LinkedList<(int key, int value)>();
            this.cache = new Dictionary<int, LinkedListNode<(int key, int value)>>();
        }

        public int Get(int key)
        {
            if (!cache.TryGetValue(key, out var node))
                return -1;

            timer.Remove(node);
            timer.AddLast(node);
            return node.Value.value;
        }

        public void Put(int key, int value)
        {
            if (cache.TryGetValue(key, out var existing))
            {
                timer.Remove(existing);
            }
            else if (cache.Count >= capacity)
            {
                var lru = timer.First;
                timer.RemoveFirst();
                cache.Remove(lru.Value.key);
            }

            var node = new LinkedListNode<(int key, int value)>((key, value));
            timer.AddLast(node);
            cache[key] = node;
        }
    }
}