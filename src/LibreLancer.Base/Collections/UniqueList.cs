using System.Collections;
using System.Collections.Generic;

namespace LibreLancer;

public class UniqueList<T> : IList<T>
{
    private List<T> _backing = new List<T>();
    
    public IEnumerator<T> GetEnumerator()
    {
        return _backing.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) _backing).GetEnumerator();
    }

    public void Add(T item)
    {
        if(!_backing.Contains(item))
            _backing.Add(item);
    }

    public void AddRange(IEnumerable<T> items)
    {
        foreach(var i in items)
            Add(i);
    }

    public void Clear()
    {
        _backing.Clear();
    }

    public bool Contains(T item)
    {
        return _backing.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _backing.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return _backing.Remove(item);
    }

    public int Count => _backing.Count;

    public bool IsReadOnly => false;

    public int IndexOf(T item)
    {
        return _backing.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        if(!_backing.Contains(item))
            _backing.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        _backing.RemoveAt(index);
    }

    public T this[int index]
    {
        get => _backing[index];
        set => _backing[index] = value;
    }
}