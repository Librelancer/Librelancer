using System;
using System.Collections;
using System.Collections.Generic;
using LibreLancer.Data;

namespace LibreLancer;

public sealed class ModelPartCollection : IEnumerable<KeyValuePair<string, RigidModelPart>>
{
    Dictionary<string, RigidModelPart> partsByName = new (StringComparer.OrdinalIgnoreCase);
    Dictionary<uint, RigidModelPart> partsByCrc = new ();
    public void Add(RigidModelPart part)
    {
        partsByName.Add(part.Name, part);
        partsByCrc.Add(CrcTool.FLModelCrc(part.Name), part);
    }

    public bool TryGetPart(string name, out RigidModelPart part) => partsByName.TryGetValue(name, out part);
    public bool TryGetPart(uint crc, out RigidModelPart part) => partsByCrc.TryGetValue(crc, out part);

    public RigidModelPart this[string name] => partsByName[name];
    public RigidModelPart this[uint crc] => partsByCrc[crc];

    public int Count => partsByCrc.Count;

    public Dictionary<string, RigidModelPart>.Enumerator GetEnumerator() =>
        partsByName.GetEnumerator();

    IEnumerator<KeyValuePair<string, RigidModelPart>> IEnumerable<KeyValuePair<string, RigidModelPart>>.GetEnumerator() =>
        partsByName.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)partsByName).GetEnumerator();
}
