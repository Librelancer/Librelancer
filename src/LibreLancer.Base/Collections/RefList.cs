using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LibreLancer;

public class RefList<T> : IList<T>
{
    private T[] backing;
    private int count = 0;

    public struct Enumerator : IEnumerator<T>
    {
        private readonly RefList<T> _list;
        private int index;
        private T? current;

        internal Enumerator(RefList<T> list)
        {
            _list = list;
            index = 0;
            current = default;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            RefList<T> localList = _list;

            if (((uint)index < (uint)localList.count))
            {
                current = localList.backing[index];
                index++;
                return true;
            }

            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            index = _list.count + 1;
            current = default;
            return false;
        }

        public T Current => current!;

        object? IEnumerator.Current
        {
            get
            {
                if (index == 0 || index == _list.count + 1)
                {
                    throw new InvalidOperationException();
                }

                return Current;
            }
        }

        void IEnumerator.Reset()
        {

            index = 0;
            current = default;
        }
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    public RefList()
    {
        backing = [];
    }

    public RefList(int capacity)
    {
        backing = new T[capacity];
    }

    public void Add(T item)
    {
        Grow();
        backing[count++] = item;
    }

    private void Grow()
    {
        if (count + 1 > backing.Length)
        {
            var newSize = backing.Length == 0 ?
                4 : backing.Length * 2;
            Array.Resize(ref backing, newSize);
        }
    }

    public void Shrink()
    {
        if (backing.Length > count)
        {
            if (count == 0) backing = [];
            else Array.Resize(ref backing, count);
        }
    }


    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            backing.AsSpan(0, count).Clear();
        }
        count = 0;
    }

    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        backing.AsSpan(0, count).CopyTo(array.AsSpan(arrayIndex));
    }

    public ReadOnlySpan<T> AsSpan() => backing.AsSpan(0, count);

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    public int Count => count;
    public bool IsReadOnly => false;

    public int IndexOf(T item)
    {
        return Array.IndexOf(backing, item, 0, count);
    }

    public void Insert(int index, T item)
    {
        if ((uint)index > (uint)count)
        {
            throw new IndexOutOfRangeException();
        }

        if (index == count)
        {
            Add(item);
        }
        else
        {
            Grow();
            Array.Copy(backing, index, backing, index + 1, count - index);
            backing[index] = item;
            count++;
        }
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)count)
        {
            throw new ArgumentOutOfRangeException();
        }
        count--;
        if (index < count)
        {
            Array.Copy(backing, index + 1, backing, index, count - index);
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            backing[count] = default!;
        }
    }

    public ref T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)count)
            {
                throw new IndexOutOfRangeException();
            }
            return ref backing[index];
        }
    }

    T IList<T>.this[int index]
    {
        get
        {
            if ((uint)index >= (uint)count)
            {
                throw new IndexOutOfRangeException();
            }
            return backing[index];
        }
        set
        {
            if ((uint)index >= (uint)count)
            {
                throw new IndexOutOfRangeException();
            }
            backing[index] = value;
        }
    }
}
