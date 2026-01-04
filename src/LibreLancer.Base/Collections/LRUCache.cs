// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer;

public class LRUCache<Key, Value>(int capacity, Func<Key, Value> load)
    where Value : IDisposable where Key : notnull
{
    private LruPtr? lruHead;
    private LruPtr? lruTail;

    private readonly Dictionary<Key, Value> loadedValues = new();

    private void AddLoaded(Key key, Value value)
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

        lruTail!.Next = ptr;
        lruTail = ptr;
        loadedValues[key] = value;
    }

    public void UsedKey(Key key)
    {
        if (lruTail is null || !loadedValues.ContainsKey(key)) {
            return;
        }

        var ptr = lruTail;
        while (ptr is not null && !ptr.Key.Equals(key))
        {
            ptr = ptr.Previous;
        }

        if (ptr == lruTail) return;
        if (ptr == lruHead)
        {
            lruHead = ptr?.Next;

            if (ptr?.Next is not null)
            {
                ptr.Next.Previous = null;
            }
        }
        else
        {
            if (ptr is { Next: not null })
            {
                ptr.Next.Previous = ptr.Previous;

                if (ptr.Previous is not null)
                {
                    ptr.Previous.Next = ptr.Next;
                }
            }

        }

        if (ptr == null)
        {
            return;
        }

        ptr.Previous = lruTail;
        ptr.Next = null;

        lruTail.Next = ptr;
        lruTail = ptr;

    }

    public void UsedValue(Value value)
    {
        var ptr = lruTail;
        while (ptr is not null && !ptr.Value.Equals(value))
        {
            ptr = ptr.Previous;
        }
        if (ptr == null) return;
        if (ptr == lruTail) return;
        if (ptr == lruHead)
        {
            lruHead = ptr.Next;

            if (ptr.Next is not null)
            {
                ptr.Next.Previous = null;
            }
        }
        else
        {
            if (ptr.Next is not null)
            {
                ptr.Next.Previous = ptr.Previous;

                if (ptr.Previous is not null)
                {
                    ptr.Previous.Next = ptr.Next;
                }
            }

        }
        ptr.Previous = lruTail;
        ptr.Next = null;

        if (lruTail is not null)
        {
            lruTail.Next = ptr;
        }

        lruTail = ptr;
    }

    public Value Get(Key key)
    {
        if (loadedValues.TryGetValue(key, out var value))
        {
            return value;
        }

        value = load(key);
        AddLoaded(key, value);
        return value;
    }

    public IEnumerable<Value> AllValues => loadedValues.Values;

    private class LruPtr
    {
        public LruPtr? Next;
        public LruPtr? Previous;
        public required Key Key;
        public required Value Value;

        public override string ToString()
        {
            var nextStr = Next == null ? "null" : Next.Key.ToString();
            var prevStr = Previous == null ? "null" : Previous.Key.ToString();
            return $"{prevStr} -> {Key} -> {nextStr}";
        }
    }
}
