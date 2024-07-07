using System.IO.Enumeration;
using System.Numerics;
using LibreLancer;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public abstract class SysObjectModification<T>(GameObject target, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected GameObject Target = target;
    string Print(T obj) => obj?.ToString() ?? "NULL";

    public override string ToString() =>
        $"{name}: {Target.Nickname ?? "No Nickname"}\nOld: {Print(Old)}\nUpdated: {Print(Updated)}";
}

public class ObjectSetTransform(GameObject target, Transform3D old, Transform3D updated, SystemObjectList objList)
    : SysObjectModification<Transform3D>(target, old, updated, "SetTransform")
{
    public override void Set(Transform3D value)
    {
        Target.SetLocalTransform(value);
        Target.GetEditData();
        Target.UpdateDirty();
        if (objList.Selection.Count > 0 && objList.Selection[0] == Target)
            objList.SelectedTransform = value.Matrix();
    }
}

public class ObjectSetNickname(GameObject target, string old, string updated, SystemObjectList objList)
    : SysObjectModification<string>(target, old, updated, "SetNickname")
{
    public override void Set(string value)
    {
        Target.GetEditData();
        Target.Nickname = value;
        objList.Refresh();
        Target.UpdateDirty();
    }
}

public class ObjectSetIdsName(GameObject target, int old, int updated)
    : SysObjectModification<int>(target, old,updated, "SetIdsName")
{
    public override void Set(int value)
    {
        Target.GetEditData().IdsName = value;
        Target.UpdateDirty();
    }
}

public class ObjectSetVisit(GameObject target, VisitFlags old, VisitFlags updated)
    : SysObjectModification<VisitFlags>(target, old, updated, "SetVisit")
{
    public override void Set(VisitFlags value)
    {
        Target.GetEditData().Visit = value;
        Target.UpdateDirty();
    }
}

public class ObjectSetReputation(GameObject target, Faction old, Faction updated)
    : SysObjectModification<Faction>(target, old, updated, "SetReputation")
{
    public override void Set(Faction value)
    {
        Target.GetEditData().Reputation = value;
        Target.UpdateDirty();
    }
}

public class ObjectSetBase(GameObject target, Base old, Base updated)
    : SysObjectModification<Base>(target, old, updated, "SetBase")
{
    public override void Set(Base value)
    {
        Target.GetEditData().Base = value;
        Target.UpdateDirty();
    }
}

public class ObjectSetDock(GameObject target, DockAction old, DockAction updated)
    : SysObjectModification<DockAction>(target, old, updated, "SetDock")
{
    public override void Set(DockAction value)
    {
        Target.GetEditData().Dock = value;
        Target.UpdateDirty();
    }
}

public class ObjectSetComment(GameObject target, string old, string updated)
    : SysObjectModification<string>(target, old, updated, "SetComment")
{
    public override void Set(string value)
    {
        Target.GetEditData().Comment = value;
        Target.UpdateDirty();
    }
}

public class ObjectSetArchetypeLoadout(
    GameObject target, SystemEditorTab tab,
    Archetype oldArchetype, ObjectLoadout oldLoadout,
    Archetype newArchetype, ObjectLoadout newLoadout) :
    EditorModification<(Archetype Archetype, ObjectLoadout Loadout)>(
        (oldArchetype, oldLoadout),
        (newArchetype, newLoadout)
        )
{
    public override void Set((Archetype Archetype, ObjectLoadout Loadout) value)
    {
        tab.SetArchetypeLoadout(target, value.Archetype, value.Loadout);
        target.UpdateDirty();
    }

    public override string ToString()
    {
        var oldA = Old.Archetype?.Nickname ?? "NULL";
        var oldL = Old.Loadout?.Nickname ?? "NULL";
        var newA = Updated.Archetype?.Nickname ?? "NULL";
        var newL = Updated.Loadout?.Nickname ?? "NULL";
        return $"SetArchetypeLoadout: {target.Nickname}\nOld: ({oldA}, {oldL}), New: ({newA}, {newL})";
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
        if (!obj.TryGetComponent<ObjectEditData>(out var ed) ||
            !ed.IsNewObject) {
            tab.DeletedObjects.Add(obj.SystemObject);
        }
        tab.OnRemoved(obj);
    }

    public override void Undo()
    {
        obj.Register(tab.World.Physics);
        tab.World.AddObject(obj);
        if (tab.DeletedObjects.Contains(obj.SystemObject))
            tab.DeletedObjects.Remove(obj.SystemObject);
        else
            obj.GetEditData().IsNewObject = true;
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
        tab.World.InitObject(Object, false, systemObject, tab.Data.Resources, false);
        Object.GetEditData().IsNewObject = true;
        tab.RefreshObjects();
    }

    public override void Undo()
    {
        var n = Object.Nickname;
        tab.World.RemoveObject(Object);
        Object.ClearAll(tab.World.Physics);
        Object.Nickname = n;
        tab.OnRemoved(Object);
    }

    public override string ToString() => $"CreateObject: {systemObject.Nickname}, {systemObject.Archetype.Nickname}";
}
