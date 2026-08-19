using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;

namespace LibreLancer.World;

public sealed class DestructibleModel
{
    public sealed class CollisionGroupPart
    {
        public readonly uint CRC;
        public readonly RigidModelPart ModelPart;
        public readonly SeparablePart Definition;
        public readonly float MaxHealth;
        public float CurrentHealth;
        internal readonly HashSet<FuseResources> RunningFuses = [];

        internal CollisionGroupPart(RigidModelPart modelPart, SeparablePart definition)
        {
            ModelPart = modelPart;
            Definition = definition;
            CRC = CrcTool.FLModelCrc(modelPart.Name);
            MaxHealth = definition.HitPoints;
            CurrentHealth = MaxHealth;
        }

        public float HealthFraction => MaxHealth > 0
            ? MathHelper.Clamp(CurrentHealth / MaxHealth, 0, 1)
            : 1;
    }

    public readonly RigidModel RigidModel;

    public IEnumerable<uint> DestroyedParts => destroyed;
    public IEnumerable<Hardpoint> Hardpoints => hardpoints;
    public IEnumerable<CollisionGroupPart> CollisionGroups => collisionGroups.Values;
    public event Action<Hardpoint>? HardpointDestroyed;

    private readonly HashSet<uint> destroyed = [];
    private readonly HashSet<uint> destroyedChildren = [];
    private readonly HardpointCollection hardpoints = new();
    private readonly Dictionary<string, RigidModelPart> hpToPart = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, CollisionGroupPart> collisionGroups = [];

    public List<SeparablePart> SeparableParts;

    public DestructibleModel(RigidModel model, List<SeparablePart> separableParts)
    {
        RigidModel = model;
        SeparableParts = separableParts;

        foreach (var part in RigidModel.AllParts)
        {
            foreach (var hp in part.Hardpoints.Where(hp => hardpoints.TryAdd(hp)))
            {
                hpToPart[hp.Definition.Name] = part;
            }
        }

        foreach (var definition in separableParts.Where(x => x.HitPoints > 0))
        {
            var crc = CrcTool.FLModelCrc(definition.Part);
            if (RigidModel.Parts.TryGetPart(crc, out var modelPart))
            {
                collisionGroups[crc] = new CollisionGroupPart(modelPart, definition);
            }
            else
            {
                FLLog.Debug("Model", $"Collision group part '{definition.Part}' was not found in {model.Path}");
            }
        }
    }

    public bool DestroyPart(string name, out RigidModelPart? part) =>
        DestroyPart(CrcTool.FLModelCrc(name), out part);

    public bool IsPartDestroyed(uint crc) => destroyed.Contains(crc);

    public bool TryGetCollisionGroup(uint crc, [MaybeNullWhen(false)] out CollisionGroupPart group) =>
        collisionGroups.TryGetValue(crc, out group);

    public bool TryGetCollisionGroup(RigidModelPart part, [MaybeNullWhen(false)] out CollisionGroupPart group) =>
        collisionGroups.TryGetValue(CrcTool.FLModelCrc(part.Name), out group) &&
        ReferenceEquals(group.ModelPart, part);

    public bool DamagePart(CollisionGroupPart group, float damage, bool invulnerable)
    {
        if (!group.ModelPart.Active || group.CurrentHealth <= 0 ||
            !collisionGroups.TryGetValue(group.CRC, out var registered) ||
            !ReferenceEquals(group, registered))
        {
            return false;
        }

        group.CurrentHealth = MathHelper.Clamp(group.CurrentHealth - damage, 0, group.MaxHealth);
        if (invulnerable)
        {
            group.CurrentHealth = Math.Max(group.CurrentHealth, group.MaxHealth * 0.09f);
        }
        return group.CurrentHealth <= 0;
    }

    public bool SetPartHealth(uint crc, float healthFraction)
    {
        if (!collisionGroups.TryGetValue(crc, out var group))
        {
            return false;
        }
        group.CurrentHealth = group.MaxHealth * MathHelper.Clamp(healthFraction, 0, 1);
        return true;
    }

    private void MarkCollisionGroupDestroyed(RigidModelPart part)
    {
        if (collisionGroups.TryGetValue(CrcTool.FLModelCrc(part.Name), out var group))
        {
            group.CurrentHealth = 0;
        }
    }

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
            MarkCollisionGroupDestroyed(c);

            foreach (var hp in c.Hardpoints.Where(hp => hpToPart[hp.Name] == part))
            {
                hardpoints.Remove(hp);
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

            hardpoints.Remove(hp);
            HardpointDestroyed?.Invoke(hp);
        }

        destroyed.Add(crc);
        MarkCollisionGroupDestroyed(part);
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

    public bool TryGetHardpoint(uint crc, [MaybeNullWhen(false)] out Hardpoint hardpoint) =>
        hardpoints.TryGetValue(crc, out hardpoint);


    public bool HardpointExists(string? hpName)
    {
        return hpName != null && hardpoints.ContainsKey(hpName);
    }
}
