using System;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Universe;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LancerEdit.GameContent;

public abstract class SysZoneModification<T>(Zone target, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected Zone Target = target;
    string Print(T obj) => obj?.ToString() ?? "NULL";

    public override string ToString() =>
        $"{name}: {Target.Nickname ?? "No Nickname"}\nOld: {Print(Old)}\nUpdated: {Print(Updated)}";
}


public class SysZoneCreate(SystemEditorTab tab, string nickname, Vector3 position) : EditorAction
{
    private Zone z;
    public EditZone Zone;

    public SysZoneCreate(SystemEditorTab t, Zone z) : this(t, z.Nickname, z.Position)
    {
        this.z = z;
    }

    public override void Commit()
    {
        if (z == null)
        {
            z = new Zone
            {
                Size = new Vector3(100, 0, 0),
                Shape = ShapeKind.Sphere,
                RotationMatrix = Matrix4x4.Identity,
                DensityRestrictions = [],
                Encounters = []
            };
        }
        z.Nickname = nickname;
        z.Position = position;
        Zone = tab.ZoneList.AddZone(z);
    }

    public override void Undo()
    {
        tab.ZoneList.RemoveZone(Zone);
    }
}

public class SysZoneSetNickname(Zone target, SystemEditorTab tab, string old, string updated)
    : SysZoneModification<string>(target, old, updated, "SetZoneNickname")
{
    public override void Set(string value)
    {
        var existing = Target.Nickname;
        Target.Nickname = value;
        tab.ZoneList.ZoneRenamed(target, existing);
        tab.ZoneList.CheckDirty();
    }
}

public class SysZoneSetIdsName(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneIdsName")
{
    public override void Set(int value)
    {
        Target.IdsName = value;
        tab.ZoneList.CheckDirty();
    }
}

public class SysZoneSetIdsInfo(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneIdsInfo")
{
    public override void Set(int value)
    {
        Target.IdsInfo = value;
        tab.ZoneList.CheckDirty();
    }
}

public class SysZoneSetPosition(Zone target, SystemEditorTab tab, Vector3 old, Vector3 updated)
    : SysZoneModification<Vector3>(target, old, updated, "SetZonePosition")
{
    public override void Set(Vector3 value)
    {
        Target.Position = value;
        tab.ZoneList.CheckDirty();
        tab.ZoneList.ZonesByPosition.UpdatePositions();
        tab.World.Renderer.ZoneVersion++;
    }
}

public class SysZoneSetRotation(
    Zone target,
    SystemEditorTab tab,
    Matrix4x4 old,
    Vector3 oldAngles,
    Matrix4x4 updated,
    Vector3 updatedAngles)
    : SysZoneModification<(Matrix4x4 Matrix, Vector3 Euler)>(target, (old, oldAngles), (updated, updatedAngles),
        "SetZoneRotation")
{

    public static SysZoneSetRotation Create(Zone target, SystemEditorTab tab, Matrix4x4 rotation) =>
        new (target, tab, target.RotationMatrix, target.RotationAngles, rotation, rotation.GetEulerDegrees());
    public override void Set((Matrix4x4 Matrix, Vector3 Euler) value)
    {
        Target.RotationMatrix = value.Matrix;
        Target.RotationAngles = value.Euler;
        tab.ZoneList.CheckDirty();
        tab.ZoneList.ZonesByPosition.UpdatePositions();
        tab.World.Renderer.ZoneVersion++;
    }
}

public class SysZoneSetComment(Zone target, SystemEditorTab tab, string old, string updated)
    : SysZoneModification<string>(target, old, updated, "SetZoneComment")
{
    public override void Set(string value)
    {
        Target.Comment = value;
        tab.ZoneList.CheckDirty();
    }
}

public class SysZoneSetShape(Zone target, SystemEditorTab tab, ShapeKind oldShape, Vector3 oldSize, ShapeKind updatedShape, Vector3 updatedSize)
    : SysZoneModification<(ShapeKind Shape, Vector3 Size)>(target, (oldShape, oldSize), (updatedShape, updatedSize), "SetZoneShape")
{
    public override void Set((ShapeKind Shape, Vector3 Size) value)
    {
        Target.Shape = value.Shape;
        Target.Size = value.Size;
        tab.ZoneList.CheckDirty();
        tab.ZoneList.ZonesByPosition.UpdatePositions();
        tab.World.Renderer.ZoneVersion++;
    }
}

public class SysZoneSetSizeX(Zone target, SystemEditorTab tab, float old, float updated)
    : SysZoneModification<float>(target, old, updated, "SetZoneSizeX")
{
    public override void Set(float value)
    {
        Target.Size = Target.Size with { X = value };
        tab.ZoneList.CheckDirty();
        tab.ZoneList.ZonesByPosition.UpdatePositions();
        tab.World.Renderer.ZoneVersion++;
    }
}

public class SysZoneSetSizeY(Zone target, SystemEditorTab tab, float old, float updated)
    : SysZoneModification<float>(target, old, updated, "SetZoneSizeY")
{
    public override void Set(float value)
    {
        Target.Size = Target.Size with { Y = value };
        tab.ZoneList.CheckDirty();
        tab.ZoneList.ZonesByPosition.UpdatePositions();
        tab.World.Renderer.ZoneVersion++;
    }
}

public class SysZoneSetSizeZ(Zone target, SystemEditorTab tab, float old, float updated)
    : SysZoneModification<float>(target, old, updated, "SetZoneSizeZ")
{
    public override void Set(float value)
    {
        Target.Size = Target.Size with { Z = value };
        tab.ZoneList.CheckDirty();
        tab.ZoneList.ZonesByPosition.UpdatePositions();
        tab.World.Renderer.ZoneVersion++;
    }
}

public class SysAddZoneAction(SystemEditorTab tab, Zone zone) : EditorAction
{
    private EditZone z;
    public override void Commit()
    {
        z = tab.ZoneList.AddZone(zone);
    }

    public override void Undo()
    {
        tab.ZoneList.RemoveZone(z);
    }
}
