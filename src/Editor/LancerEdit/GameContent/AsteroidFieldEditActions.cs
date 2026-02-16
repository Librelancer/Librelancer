using System.Collections.Generic;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent;


public class FieldFlagModification(AsteroidField target, FieldFlags flag, bool newValue)
    : EditorFlagModification<AsteroidField, FieldFlags>(flag, newValue)
{
    public override ref FieldFlags Field => ref target.Flags;
}

public class AsteroidFieldSetDiffuseColor(AsteroidField target, Color4 oldValue, Color4 newValue)
    : EditorModification<Color4>(oldValue, newValue)
{
    public override void Set(Color4 value) => target.DiffuseColor = value;
}

public class AsteroidFieldSetAmbientColor(AsteroidField target, Color4 oldValue, Color4 newValue)
    : EditorModification<Color4>(oldValue, newValue)
{
    public override void Set(Color4 value) => target.AmbientColor = value;
}

public class AsteroidFieldSetAmbientIncrease(AsteroidField target, Color4 oldValue, Color4 newValue)
    : EditorModification<Color4>(oldValue, newValue)
{
    public override void Set(Color4 value) => target.AmbientIncrease = value;
}

public class AsteroidFieldSetFillDist(AsteroidField target, float oldValue, float newValue)
    : EditorModification<float>(oldValue, newValue)
{
    public override void Set(float value) => target.FillDist = value;
}

public class AsteroidFieldSetEmptyCubeFrequency(AsteroidField target, float oldValue, float newValue)
    : EditorModification<float>(oldValue, newValue)
{
    public override void Set(float value) => target.EmptyCubeFrequency = value;
}

public class AsteroidFieldSetCubeSize(AsteroidField target, int oldValue, int newValue)
    : EditorModification<int>(oldValue, newValue)
{
    public override void Set(int value) => target.CubeSize = value;
}

public class AsteroidFieldSetCubeRotation(AsteroidField target, AsteroidCubeRotation oldValue, AsteroidCubeRotation newValue)
    : EditorModification<AsteroidCubeRotation>(oldValue, newValue)
{
    public override void Set(AsteroidCubeRotation value) => target.CubeRotation = value;
}

public class AsteroidFieldSetCube(AsteroidField target, List<StaticAsteroid> oldValue, List<StaticAsteroid> newValue)
    : EditorModification<List<StaticAsteroid>>(oldValue, newValue)
{
    public override void Set(List<StaticAsteroid> value) => target.Cube = value;
}

public class AsteroidFieldSetBillboardCount(AsteroidField target, int oldValue, int newValue)
    : EditorModification<int>(oldValue, newValue)
{
    public override void Set(int value) => target.BillboardCount = value;
}

public class AsteroidFieldSetBillboardDistance(AsteroidField target, float oldValue, float newValue)
    : EditorModification<float>(oldValue, newValue)
{
    public override void Set(float value) => target.BillboardDistance = value;
}

public class AsteroidFieldSetBillboardFadePercentage(AsteroidField target, float oldValue, float newValue)
    : EditorModification<float>(oldValue, newValue)
{
    public override void Set(float value) => target.FillDist = value;
}

public class AsteroidFieldSetBillboardShape(AsteroidField target, string oldValue, string newValue)
    : EditorModification<string>(oldValue, newValue)
{
    public override void Set(string value) => target.BillboardShape = value;
}

public class AsteroidFieldSetBillboardSize(AsteroidField target, Vector2 oldValue, Vector2 newValue)
    : EditorModification<Vector2>(oldValue, newValue)
{
    public override void Set(Vector2 value) => target.BillboardSize = value;
}

public class AsteroidFieldSetBillboardTint(AsteroidField target, Color3f oldValue, Color3f newValue)
    : EditorModification<Color3f>(oldValue, newValue)
{
    public override void Set(Color3f value) => target.BillboardTint = value;
}

public class AsteroidFieldSetBand(AsteroidField target, AsteroidBand oldValue, AsteroidBand newValue)
    : EditorModification<AsteroidBand>(oldValue, newValue)
{
    public override void Set(AsteroidBand value) => target.Band = value;
}

public class AsteroidFieldSetDynamicAsteroids(AsteroidField target, List<DynamicAsteroids> oldValue, List<DynamicAsteroids> newValue)
    : EditorModification<List<DynamicAsteroids>>(oldValue, newValue)
{
    public override void Set(List<DynamicAsteroids> value) => target.DynamicAsteroids = value;
}

public class AsteroidFieldSetExclusionZones(AsteroidField target, List<AsteroidExclusionZone> oldValue, List<AsteroidExclusionZone> newValue)
    : EditorModification<List<AsteroidExclusionZone>>(oldValue, newValue)
{
    public override void Set(List<AsteroidExclusionZone> value) => target.ExclusionZones = value;
}

public class AsteroidFieldSetFieldLoot(AsteroidField target, DynamicLootZone oldValue, DynamicLootZone newValue)
    : EditorModification<DynamicLootZone>(oldValue, newValue)
{
    public override void Set(DynamicLootZone value) => target.FieldLoot = value;
}

public class AsteroidFieldSetLootZones(AsteroidField target, List<DynamicLootZone> oldValue, List<DynamicLootZone> newValue)
    : EditorModification<List<DynamicLootZone>>(oldValue, newValue)
{
    public override void Set(List<DynamicLootZone> value) => target.LootZones = value;
}

public class AsteroidFieldRefresh(SystemEditorTab tab) : EditorAction
{
    public override void Commit()
    {
        tab.ReloadFieldRenderers();
    }

    public override void Undo()
    {
        tab.ReloadFieldRenderers();
    }

    public override string ToString() => "(Reload Asteroid Fields)";
}

public class SysAddAsteroidField(AsteroidField newField, SystemEditorTab tab)
    : EditorAction
{
    public override void Commit()
    {
        tab.ZoneList.AsteroidFields.Fields.Add(newField);
        tab.ZoneList.AsteroidFields.OriginalFields[newField] = null;
        tab.ReloadFieldRenderers();

    }

    public override void Undo()
    {
        tab.ZoneList.AsteroidFields.Fields.Remove(newField);
        tab.ZoneList.AsteroidFields.OriginalFields.Remove(newField);
        tab.ReloadFieldRenderers();
    }
}
