using System;
using System.Collections;
using System.Collections.Generic;

namespace LibreLancer.Data;

public class GameItemCollection<T> : IEnumerable<T> where T : IdentifiableItem
{
    private Dictionary<string, T> nicknameCollection = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<uint, T> crcCollection = new();

    public IEnumerable<uint> Crcs => crcCollection.Keys;

    public int Count => crcCollection.Count;

    public bool TryGetValue(string? nickname, out T? value)
    {
        if (!string.IsNullOrEmpty(nickname))
        {
            return nicknameCollection.TryGetValue(nickname, out value);
        }

        value = null;
        return false;
    }

    public bool TryGetValue(uint crc, out T? value) => crcCollection.TryGetValue(crc, out value);

    public T? Get(string? nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            return null;
        }

        nicknameCollection.TryGetValue(nickname, out var result);
        return result;
    }

    public T? Get(uint crc)
    {
        crcCollection.TryGetValue(crc, out var result);
        return result;
    }

    public T? Get(int crc) => Get((uint) crc);

    public bool Contains(string nickname) => nicknameCollection.ContainsKey(nickname);

    public bool Contains(uint crc) => crcCollection.ContainsKey(crc);

    public void Add(T item)
    {
        if (string.IsNullOrEmpty(item.Nickname) || item.CRC == 0)
        {
            throw new ArgumentNullException();
        }

        if (crcCollection.ContainsKey(item.CRC) && !nicknameCollection.ContainsKey(item.Nickname))
        {
            throw new ArgumentException(
                $"CRC collision between '{item.Nickname}' and {crcCollection[item.CRC].Nickname}");
        }

        nicknameCollection[item.Nickname] = item;
        crcCollection[item.CRC] = item;
    }

    public IEnumerator<T> GetEnumerator() => nicknameCollection.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) nicknameCollection.Values).GetEnumerator();
}
