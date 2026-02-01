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
    }
}

public class SysZoneSetIdsName(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneIdsName")
{
    public override void Set(int value)
    {
        Target.IdsName = value;
    }
}

public class SysZoneSetIdsInfo(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneIdsInfo")
{
    public override void Set(int value)
    {
        Target.IdsInfo = value;
    }
}

public class SysZoneSetPosition(Zone target, SystemEditorTab tab, Vector3 old, Vector3 updated)
    : SysZoneModification<Vector3>(target, old, updated, "SetZonePosition")
{
    public override void Set(Vector3 value)
    {
        Target.Position = value;
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
    }
}

public class SysZoneSetShape(Zone target, SystemEditorTab tab, ShapeKind oldShape, Vector3 oldSize, ShapeKind updatedShape, Vector3 updatedSize)
    : SysZoneModification<(ShapeKind Shape, Vector3 Size)>(target, (oldShape, oldSize), (updatedShape, updatedSize), "SetZoneShape")
{
    public override void Set((ShapeKind Shape, Vector3 Size) value)
    {
        Target.Shape = value.Shape;
        Target.Size = value.Size;
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
        tab.ZoneList.ZonesByPosition.UpdatePositions();
        tab.World.Renderer.ZoneVersion++;
    }
}

public class SysZoneSetSort(Zone target, SystemEditorTab tab, float old, float updated)
    : SysZoneModification<float>(target, old, updated, "SetZoneSort")
{
    public override void Set(float value) => Target.Sort = value;
}

public class SysZoneSetToughness(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneToughness")
{
    public override void Set(int value) => Target.Toughness = value;
}

public class SysZoneSetDensity(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneDensity")
{
    public override void Set(int value) => Target.Density = value;
}

public class SysZoneSetRepopTime(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneRepopTime")
{
    public override void Set(int value) => Target.RepopTime = value;
}

public class SysZoneSetMaxBattleSize(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneMaxBattleSize")
{
    public override void Set(int value) => Target.MaxBattleSize = value;
}

public class SysZoneSetReliefTime(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneReliefTime")
{
    public override void Set(int value) => Target.ReliefTime = value;
}

public class SysZoneSetPropertyFlags(Zone target, SystemEditorTab tab, ZonePropFlags old, ZonePropFlags updated)
    : SysZoneModification<ZonePropFlags>(target, old, updated, "SetZonePropertyFlags")
{
    public override void Set(ZonePropFlags value)
    {
        var oldHadCloud = (old & ZonePropFlags.Cloud) == ZonePropFlags.Cloud;
        var newHasCloud = (value & ZonePropFlags.Cloud) == ZonePropFlags.Cloud;
        
        Target.PropertyFlags = value;
        
        // If the Cloud/Nebula flag changed, reload the renderers
        if (oldHadCloud != newHasCloud)
        {
            tab.ReloadFieldRenderers();
        }
    }
}

public class SysZoneSetVisitFlags(Zone target, SystemEditorTab tab, VisitFlags old, VisitFlags updated)
    : SysZoneModification<VisitFlags>(target, old, updated, "SetZoneVisitFlags")
{
    public override void Set(VisitFlags value) => Target.VisitFlags = value;
}

public class SysZoneSetSpacedustMaxParticles(Zone target, SystemEditorTab tab, int old, int updated)
    : SysZoneModification<int>(target, old, updated, "SetZoneSpacedustMaxParticles")
{
    public override void Set(int value) => Target.SpacedustMaxParticles = value;
}

public class SysZoneSetPopType(Zone target, SystemEditorTab tab, string[] old, string[] updated)
    : SysZoneModification<string[]>(target, old, updated, "SetZonePopType")
{
    public override void Set(string[] value) => Target.PopType = value;
}

public class SysZoneSetSpacedust(Zone target, SystemEditorTab tab, string old, string updated)
    : SysZoneModification<string>(target, old, updated, "SetZoneSpacedust")
{
    public override void Set(string value) => Target.Spacedust = value;
}

public class SysZoneSetMusic(Zone target, SystemEditorTab tab, string old, string updated)
    : SysZoneModification<string>(target, old, updated, "SetZoneMusic")
{
    public override void Set(string value) => Target.Music = value;
}

public class SysZoneSetPropertyFogColor(Zone target, SystemEditorTab tab, Color4? old, Color4? updated)
    : SysZoneModification<Color4?>(target, old, updated, "SetZonePropertyFogColor")
{
    public override void Set(Color4? value) => Target.PropertyFogColor = value;
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
