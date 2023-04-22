// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;

namespace LibreLancer
{
    public class LRUCache<Key,Value> where Value : IDisposable
    {
        private Func<Key, Value> load;
        private int capacity;

        private LruPtr lruHead;
        private LruPtr lruTail;
        
        Dictionary<Key,Value> loadedValues = new Dictionary<Key, Value>();
        
        public LRUCache(int capacity, Func<Key, Value> load)
        {
            this.capacity = capacity;
            this.load = load;
        }

        void AddLoaded(Key key, Value value)
        {
            if (lruHead == null)
            {
                lruHead = lruTail = new LruPtr() {Key = key, Value = value};
                loadedValues[key] = value;
                return;
            }
            LruPtr ptr;
            if (loadedValues.Count == capacity)
            {
                //Evict oldest and reuse ptr object
                var h = lruHead;
                h.Value.Dispose();
                loadedValues.Remove(h.Key);
                lruHead = h.Next;
                ptr = h;
                ptr.Key = key;
                ptr.Value = value;
                ptr.Next = null;
                ptr.Previous = lruTail;
            }
            else
            {
                ptr = new LruPtr() {
                    Key = key, Value = value, Previous = lruTail
                };
            }
            lruTail.Next = ptr;
            lruTail = ptr;
            loadedValues[key] = value;
        }

        public void UsedKey(Key key)
        {
            if (!loadedValues.ContainsKey(key)) {
                return;
            }
            var ptr = lruTail;
            while (!ptr.Key.Equals(key))
            {
                ptr = ptr.Previous;
            }
            if (ptr == lruTail) return;
            if (ptr == lruHead)
            {
                lruHead = ptr.Next;
                ptr.Next.Previous = null;
            }
            else
            {
                ptr.Next.Previous = ptr.Previous;
                ptr.Previous.Next = ptr.Next;
            }
            ptr.Previous = lruTail;
            ptr.Next = null;
            lruTail.Next = ptr;
            lruTail = ptr;
        }
        public void UsedValue(Value value)
        {
            var ptr = lruTail;
            while (ptr != null && !ptr.Value.Equals(value))
            {
                ptr = ptr.Previous;
            }
            if (ptr == null) return;
            if (ptr == lruTail) return;
            if (ptr == lruHead)
            {
                lruHead = ptr.Next;
                ptr.Next.Previous = null;
            }
            else
            {
                ptr.Next.Previous = ptr.Previous;
                ptr.Previous.Next = ptr.Next;
            }
            ptr.Previous = lruTail;
            ptr.Next = null;
            lruTail.Next = ptr;
            lruTail = ptr;
        }

        public Value Get(Key key)
        {
            if (loadedValues.TryGetValue(key, out var value))
                return value;
            value = load(key);
            AddLoaded(key, value);
            return value;
        }

        public IEnumerable<Value> AllValues => loadedValues.Values;

        class LruPtr
        {
            public LruPtr Next;
            public LruPtr Previous;
            public Key Key;
            public Value Value;
            
            public override string ToString()
            {
                string nextStr = Next == null ? "null" : Next.Key.ToString();
                string prevStr = Previous == null ? "null" : Previous.Key.ToString();
                return $"{prevStr} -> {Key} -> {nextStr}";
            }
        }
    }
    
}