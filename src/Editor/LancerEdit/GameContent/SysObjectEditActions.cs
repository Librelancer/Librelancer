using System.IO.Enumeration;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;
using Faction = LibreLancer.Data.GameData.Faction;

namespace LancerEdit.GameContent;

public abstract class SysObjectModification<T>(GameObject target, SystemObjectList list, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected GameObject Target = target;
    string Print(T obj) => obj?.ToString() ?? "NULL";

    public override string ToString() =>
        $"{name}: {Target.Nickname ?? "No Nickname"}\nOld: {Print(Old)}\nUpdated: {Print(Updated)}";

    public override void Set(T value)
    {
        SetData(Target.SystemObject, value);
    }

    protected abstract void SetData(SystemObject data, T value);
}

public class ObjectSetTransform(GameObject target, SystemObjectList list,Transform3D old, Transform3D updated)
    : SysObjectModification<Transform3D>(target, list, old, updated, "SetTransform")
{
    protected override void SetData(SystemObject data, Transform3D value)
    {
        data.Position = value.Position;
        data.Rotation = value.Orientation;
        Target.SetLocalTransform(value);
        if (list.Selection.Count > 0 && list.Selection[0] == Target)
            list.SelectedTransform = value.Matrix();
    }
}

public class ObjectSetNickname(GameObject target, SystemObjectList list, string old, string updated)
    : SysObjectModification<string>(target, list, old, updated, "SetNickname")
{
    protected override void SetData(SystemObject data, string value)
    {
        Target.Nickname = value;
        data.Nickname = value;
        list.Refresh();
    }
}


public class ObjectSetArchetypeLoadoutStar(
    GameObject target, SystemEditorTab tab,
    LibreLancer.Data.GameData.Archetype oldArchetype, ObjectLoadout oldLoadout, Sun oldSun,
    LibreLancer.Data.GameData.Archetype newArchetype, ObjectLoadout newLoadout, Sun newSun) :
    EditorModification<(LibreLancer.Data.GameData.Archetype Archetype, ObjectLoadout Loadout, Sun Star)>(
        (oldArchetype, oldLoadout, oldSun),
        (newArchetype, newLoadout, newSun)
        )
{
    public override void Set((LibreLancer.Data.GameData.Archetype Archetype, ObjectLoadout Loadout, Sun Star) value)
    {
        tab.SetArchetypeLoadout(target, value.Archetype, value.Loadout, value.Star);
    }

    public override string ToString()
    {
        var oldA = Old.Archetype?.Nickname ?? "NULL";
        var oldL = Old.Loadout?.Nickname ?? "NULL";
        var oldS = Old.Star?.Nickname ?? "NULL";
        var newA = Updated.Archetype?.Nickname ?? "NULL";
        var newL = Updated.Loadout?.Nickname ?? "NULL";
        var newS = Updated.Star?.Nickname ?? "NULL";
        return $"SetArchetypeLoadoutStar: {target.Nickname}\nOld: ({oldA}, {oldL}, {oldS}), New: ({newA}, {newL}, {newS})";
    }
}

public class SysDeleteObject : EditorAction
{
    private SystemEditorTab tab;
    private GameObject obj;

    public SysDeleteObject(SystemEditorTab tab, GameObject obj)
    {
        this.tab = tab;
        this.obj = obj;
    }

    public override void Commit()
    {
        obj.Unregister(tab.World.Physics);
        tab.World.RemoveObject(obj);
        tab.OnRemoved(obj);
    }

    public override void Undo()
    {
        obj.Register(tab.World.Physics);
        tab.World.AddObject(obj);
        tab.RefreshObjects();
    }

    public override string ToString() => $"DeleteObject: {obj.Nickname}";
}
public class SysCreateObject : EditorAction
{
    public GameObject Object;

    private SystemEditorTab tab;
    private SystemObject systemObject;

    public SysCreateObject(SystemEditorTab tab, SystemObject systemObject)
    {
        Object = new GameObject();
        this.tab = tab;
        this.systemObject = systemObject;
    }
    public override void Commit()
    {
        tab.World.InitObject(Object, false, systemObject, tab.Data.Resources, null, false);
        tab.ObjectsList.Refresh();
    }

    public override void Undo()
    {
        var n = Object.Nickname;
        tab.World.RemoveObject(Object);
        Object.ClearAll(tab.World.Physics);
        Object.Nickname = n;
        tab.OnRemoved(Object);
        tab.ObjectsList.Refresh();
    }

    public override string ToString() => $"CreateObject: {systemObject.Nickname}, {systemObject.Archetype.Nickname}";
}
