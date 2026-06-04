using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LibreLancer.World;

public class HardpointCollection : IEnumerable<Hardpoint>
{
    private Dictionary<string, Hardpoint> byName = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<uint, Hardpoint> byId = new();

    public bool TryAdd(Hardpoint hardpoint)
    {
        if (!byName.TryAdd(hardpoint.Name, hardpoint))
        {
            return false;
        }
        byId[hardpoint.CRC] = hardpoint;
        return true;
    }

    public void Add(Hardpoint hardpoint)
    {
        byName[hardpoint.Name] = hardpoint;
        byId[hardpoint.CRC] = hardpoint;
    }

    public bool Remove(Hardpoint hardpoint)
    {
        var r = byName.Remove(hardpoint.Name);
        byId.Remove(hardpoint.CRC);
        return r;
    }

    public bool ContainsKey(string? name)
    {
        if (name == null)
            return false;
        return byName.ContainsKey(name);
    }


    public bool TryGetValue(string? name, [MaybeNullWhen(false)] out Hardpoint hardpoint)
    {
        if (name == null)
        {
            hardpoint = null;
            return false;
        }
        return byName.TryGetValue(name, out hardpoint);
    }

    public bool TryGetValue(uint name, [MaybeNullWhen(false)]out Hardpoint hardpoint) => byId.TryGetValue(name, out hardpoint);

    public Hardpoint this[string name] => byName[name];

    public Hardpoint this[uint id] => byId[id];

    public Dictionary<string, Hardpoint>.ValueCollection.Enumerator GetEnumerator() => byName.Values.GetEnumerator();

    IEnumerator<Hardpoint> IEnumerable<Hardpoint>.GetEnumerator() => byName.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => byName.Values.GetEnumerator();
}
