using System;
using System.Collections.Generic;
using LibreLancer.Data;
using LibreLancer.Data.GameData;

namespace LibreLancer.World;

public sealed class DestructibleModel
{
    public readonly RigidModel RigidModel;

    public IEnumerable<uint> DestroyedParts => destroyed;
    public IEnumerable<Hardpoint> Hardpoints => hardpoints.Values;
    public event Action<Hardpoint> HardpointDestroyed;

    HashSet<uint> destroyed = new HashSet<uint>();
    private HashSet<uint> destroyedChildren = new HashSet<uint>();
    Dictionary<string, Hardpoint> hardpoints = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, RigidModelPart> hpToPart = new(StringComparer.OrdinalIgnoreCase);

    public List<SeparablePart> SeparableParts;


    public DestructibleModel(RigidModel model, List<SeparablePart> separableParts)
    {
        RigidModel = model;
        SeparableParts = separableParts;
        foreach (var part in RigidModel.AllParts)
        {
            foreach (var hp in part.Hardpoints)
            {
                if (hardpoints.TryAdd(hp.Definition.Name, hp))
                {
                    hpToPart[hp.Definition.Name] = part;
                }
            }
        }
    }

    public bool DestroyPart(string name, out RigidModelPart part) =>
        DestroyPart(CrcTool.FLModelCrc(name), out part);

    public bool IsPartDestroyed(uint crc) => destroyed.Contains(crc);

    void CascadeDestroy(RigidModelPart part)
    {
        foreach (var c in part.Children)
        {
            var id = CrcTool.FLModelCrc(c.Name);
            if (destroyed.Contains(id))
                continue;
            c.Active = false;
            destroyedChildren.Add(id);
            foreach (var hp in c.Hardpoints)
            {
                if(hpToPart[hp.Name] == part)
                {
                    hardpoints.Remove(hp.Name);
                    HardpointDestroyed?.Invoke(hp);
                }
            }
            CascadeDestroy(c);
        }
    }

    public bool DestroyPart(uint crc, out RigidModelPart part)
    {
        if (destroyed.Contains(crc) ||
            destroyedChildren.Contains(crc) ||
            !RigidModel.Parts.TryGetPart(crc, out part))
        {
            part = null;
            return false;
        }

        foreach (var hp in part.Hardpoints)
        {
            if(hpToPart[hp.Name] == part)
            {
                hardpoints.Remove(hp.Name);
                HardpointDestroyed?.Invoke(hp);
            }
        }

        destroyed.Add(crc);
        part.Active = false;
        CascadeDestroy(part);
        return true;
    }


    public bool TryGetHardpoint(string hpname, out Hardpoint hardpoint)
    {
        if (hpname == null)
        {
            hardpoint = null;
            return false;
        }
        return hardpoints.TryGetValue(hpname, out hardpoint);
    }


    public bool HardpointExists(string hpname)
    {
        return hpname != null && hardpoints.ContainsKey(hpname);
    }

}
