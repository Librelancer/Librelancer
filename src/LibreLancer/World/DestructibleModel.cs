using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;

namespace LibreLancer.World;

public sealed class DestructibleModel
{
    public readonly RigidModel RigidModel;

    public IEnumerable<uint> DestroyedParts => destroyed;
    public IEnumerable<Hardpoint> Hardpoints => hardpoints.Values;
    public event Action<Hardpoint>? HardpointDestroyed;

    private readonly HashSet<uint> destroyed = [];
    private readonly HashSet<uint> destroyedChildren = [];
    private readonly Dictionary<string, Hardpoint> hardpoints = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RigidModelPart> hpToPart = new(StringComparer.OrdinalIgnoreCase);

    public List<SeparablePart> SeparableParts;

    public DestructibleModel(RigidModel model, List<SeparablePart> separableParts)
    {
        RigidModel = model;
        SeparableParts = separableParts;

        foreach (var part in RigidModel.AllParts)
        {
            foreach (var hp in part.Hardpoints.Where(hp => hardpoints.TryAdd(hp.Definition.Name, hp)))
            {
                hpToPart[hp.Definition.Name] = part;
            }
        }
    }

    public bool DestroyPart(string name, out RigidModelPart? part) =>
        DestroyPart(CrcTool.FLModelCrc(name), out part);

    public bool IsPartDestroyed(uint crc) => destroyed.Contains(crc);

    private void CascadeDestroy(RigidModelPart part)
    {
        if (part.Children is null)
        {
            return;
        }

        foreach (var c in part.Children)
        {
            var id = CrcTool.FLModelCrc(c.Name);

            if (destroyed.Contains(id))
            {
                continue;
            }

            c.Active = false;
            destroyedChildren.Add(id);

            foreach (var hp in c.Hardpoints.Where(hp => hpToPart[hp.Name] == part))
            {
                hardpoints.Remove(hp.Name);
                HardpointDestroyed?.Invoke(hp);
            }

            CascadeDestroy(c);
        }
    }

    public bool DestroyPart(uint crc, [MaybeNullWhen(false)] out RigidModelPart part)
    {
        var foundPart = RigidModel.Parts!.TryGetPart(crc, out part);
        if (destroyed.Contains(crc) || destroyedChildren.Contains(crc) || !foundPart)
        {
            part = null;
            return false;
        }

        foreach (var hp in part!.Hardpoints)
        {
            if (hpToPart[hp.Name] != part)
            {
                continue;
            }

            hardpoints.Remove(hp.Name);
            HardpointDestroyed?.Invoke(hp);
        }

        destroyed.Add(crc);
        part.Active = false;
        CascadeDestroy(part);
        return true;
    }

    public bool TryGetHardpoint(string? hpName, [MaybeNullWhen(false)] out Hardpoint hardpoint)
    {
        if (hpName != null)
        {
            return hardpoints.TryGetValue(hpName, out hardpoint);
        }

        hardpoint = null;
        return false;
    }

    public bool HardpointExists(string? hpName)
    {
        return hpName != null && hardpoints.ContainsKey(hpName);
    }
}
