using System;
using System.Collections.Concurrent;

namespace LibreLancer.Data.Ini;

/// <summary>
/// Class that de-duplicates ReadOnlySpan of char into string objects
/// </summary>
public sealed class IniStringPool
{
    private ConcurrentDictionary<string, string> strings;
    private ConcurrentDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> spanLookup;

    public IniStringPool()
    {
        strings = new();
        if (!strings.TryGetAlternateLookup(out spanLookup))
        {
            throw new InvalidOperationException(); //Should never happen
        }
    }

    private static string Factory(string key) => key;

    public string FromSpan(ReadOnlySpan<char> instance)
    {
        if (!spanLookup.TryGetValue(instance, out var value))
        {
            var k = instance.ToString();
            value = strings.GetOrAdd(k, Factory);
        }
        return value;
    }
}
