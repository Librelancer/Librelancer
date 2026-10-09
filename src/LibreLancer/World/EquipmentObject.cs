using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Resources;

namespace LibreLancer.World;

public abstract class EquipmentObject
{
    public GameObject Parent;
    public Hardpoint? Attachment;
    public float Health;
    public float MaxHealth => Equipment.Hitpoints > 0 ? Equipment.Hitpoints : 1;
    public bool Invincible;

    public Equipment Equipment;

    public bool Updates;
    public bool Renders;

    protected Transform3D Offset = Transform3D.Identity;

    protected EquipmentObject(
        GameObject parent,
        Hardpoint? attachment,
        Equipment equipment,
        EquipmentType type,
        bool updates,
        bool renders)
    {
        Parent = parent;
        Attachment = attachment;
        Equipment = equipment;
        Updates = updates && (type != EquipmentType.Cutscene);
        Renders = renders && (type != EquipmentType.Server);
        if (equipment.Hitpoints > 0)
        {
            Health = equipment.Hitpoints;
            Invincible = false;
        }
        else
        {
            Health = 1;
            Invincible = true;
        }
    }

    private bool transformDirty = true;
    private int attachVersion = 0;
    private Transform3D cachedTransform;

    public Transform3D GetTransform() => GetTransform(out _);

    public Transform3D GetTransform(out bool changed)
    {
        var dirty = transformDirty || (Attachment != null && attachVersion != Attachment.ParentVersion);
        changed = dirty;
        if (dirty)
        {
            var tr = Offset * (Attachment?.Transform ?? Transform3D.Identity);
            tr *= Parent.Transform;
            cachedTransform = tr;
            transformDirty = false;
            attachVersion = Attachment?.ParentVersion ?? 0;
        }
        return cachedTransform;
    }

    public void NeedUpdateTransform()
    {
        transformDirty = true;
    }

    public bool HandleHullDamage(float hullDamage)
    {
        if (Invincible)
            return false;
        Health -= hullDamage;
        if (Health < 0)
            Health = 0;
        return Health <= 0;
    }


    public virtual bool TryGetDescription(int id, [NotNullWhen(true)] out NetShipCargo? cargo)
    {
        var health = (byte)255;
        if (!Invincible)
        {
            var value = (int)((Health / MaxHealth) * 255);
            health = (byte)Math.Clamp(value, 0, 255);
        }

        cargo = new NetShipCargo(id, Equipment.CRC, Attachment?.Name ?? "internal", health, 1);
        return true;
    }

    public virtual bool TryGetLoadoutItem(out LoadoutItem item)
    {
        var hp = Attachment?.Name ?? "internal";
        item = new LoadoutItem(hp, Equipment);
        return true;
    }

    public virtual bool TryGetModel([NotNullWhen(true)]out RigidModel? model)
    {
        model = null;
        return false;
    }

    public virtual void ResolveReferences()
    {
    }

    public virtual void OnUpdate(double delta, GameWorld world)
    {
    }

    public virtual void RenderUpdate(double delta)
    {
    }

    public virtual void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
    }

    public virtual void OnKilled(GameObject? attacker, GameWorld world)
    {
    }

    public virtual void OnRemoved()
    {
    }

    public virtual void SetOpacity(float opacity) { }

}

public abstract class EquipmentObject<T>(
    GameObject parent,
    Hardpoint? attachment,
    T equipment,
    EquipmentType type,
    bool updates,
    bool renders)
    : EquipmentObject(parent, attachment, equipment, type, updates, renders)
    where T : Equipment
{
    public new T Equipment = equipment;
}

public abstract class EquipmentModelObject<T> : EquipmentObject<T>
    where T : Equipment
{
    private ModelRenderer? renderer;
    public DestructibleModel? Model;

    protected EquipmentModelObject(
        GameObject parent,
        Hardpoint? attachment,
        T equipment,
        EquipmentType type,
        ResourceManager resources,
        bool updates) : base(parent, attachment, equipment, type, updates, true)
    {
        var src = equipment.ModelFile?.LoadFile(resources);
        if (src == null)
            return;
        if (src.Drawable is not IRigidModelFile rigidModelFile)
            return;
        var model = rigidModelFile.CreateRigidModel(type != EquipmentType.Server, resources);
        Model = new DestructibleModel(model, []);
        if (Model.TryGetHardpoint("HPChild", out var childHp))
        {
            Offset = childHp.Transform.Inverse();
        }
        if (type != EquipmentType.Server)
            renderer = new(model) { LODRanges = equipment.LODRanges };
    }

    public override void RenderUpdate(double delta)
    {
        if (renderer != null)
        {
            var tr = GetTransform();
            renderer.Update(delta, tr);
        }
    }

    public override bool TryGetModel([NotNullWhen(true)] out RigidModel? model)
    {
        model = Model?.RigidModel;
        return model != null;
    }

    protected IEnumerable<Hardpoint> GetHardpoints() => Model?.Hardpoints ?? [];

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        if (renderer != null)
        {
            renderer.PrepareRender(camera, nr, sys, parentCull);
        }
    }

    public override void SetOpacity(float opacity)
    {
        if (renderer != null)
        {
            renderer.OpacityMultiplier = opacity;
        }
    }
}
