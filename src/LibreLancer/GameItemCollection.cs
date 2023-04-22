using System;
using System.Collections;
using System.Collections.Generic;

namespace LibreLancer;

public class GameItemCollection<T> : IEnumerable<T> where T : IdentifiableItem
{
    private Dictionary<string, T> nicknameCollection = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<uint, T> crcCollection = new Dictionary<uint, T>();

    public bool TryGetValue(string nickname, out T value) => nicknameCollection.TryGetValue(nickname, out value);

    public bool TryGetValue(uint crc, out T value) => crcCollection.TryGetValue(crc, out value);

    public T Get(string nickname)
    {
        nicknameCollection.TryGetValue(nickname, out var result);
        return result;
    }

    public T Get(uint crc)
    {
        crcCollection.TryGetValue(crc, out var result);
        return result;
    }

    public T Get(int crc) => Get((uint) crc);

    public bool Contains(string nickname) => nicknameCollection.ContainsKey(nickname);

    public bool Contains(uint crc) => crcCollection.ContainsKey(crc);

    public void Add(T item)
    {
        var identity = (IdentifiableItem) item;
        nicknameCollection[item.Nickname] = item;
        crcCollection[item.CRC] = item;
    }

    public IEnumerator<T> GetEnumerator() => nicknameCollection.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) nicknameCollection.Values).GetEnumerator();
}