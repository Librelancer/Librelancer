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
        SetData(Target.GetEditData(), value);
        Target.UpdateDirty();
        list.CheckDirty();
    }

    protected abstract void SetData(ObjectEditData data, T value);
}

public class ObjectSetTransform(GameObject target, SystemObjectList list,Transform3D old, Transform3D updated)
    : SysObjectModification<Transform3D>(target, list, old, updated, "SetTransform")
{
    protected override void SetData(ObjectEditData data, Transform3D value)
    {
        Target.SetLocalTransform(value);
        if (list.Selection.Count > 0 && list.Selection[0] == Target)
            list.SelectedTransform = value.Matrix();
    }
}

public class ObjectSetNickname(GameObject target, SystemObjectList list, string old, string updated)
    : SysObjectModification<string>(target, list, old, updated, "SetNickname")
{
    protected override void SetData(ObjectEditData data, string value)
    {
        Target.Nickname = value;
        list.Refresh();
    }
}

public class ObjectSetIdsName(GameObject target, SystemObjectList list, int old, int updated)
    : SysObjectModification<int>(target, list, old,updated, "SetIdsName")
{
    protected override void SetData(ObjectEditData data, int value)
    {
        data.IdsName = value;
    }
}

public class ObjectSetIdsInfo(GameObject target, SystemObjectList list, int old, int updated)
    : SysObjectModification<int>(target, list, old,updated, "SetIdsInfo")
{
    protected override void SetData(ObjectEditData data, int value)
    {
        data.IdsInfo = value;
    }
}

public class ObjectSetVisit(GameObject target, SystemObjectList list, VisitFlags old, VisitFlags updated)
    : SysObjectModification<VisitFlags>(target, list, old, updated, "SetVisit")
{
    protected override void SetData(ObjectEditData data, VisitFlags value)
    {
        data.Visit = value;
    }
}

public class ObjectSetReputation(GameObject target, SystemObjectList list, Faction old, Faction updated)
    : SysObjectModification<Faction>(target, list, old, updated, "SetReputation")
{
    protected override void SetData(ObjectEditData data, Faction value)
    {
        data.Reputation = value;
    }
}

public class ObjectSetBase(GameObject target, SystemObjectList list, Base old, Base updated)
    : SysObjectModification<Base>(target, list, old, updated, "SetBase")
{
    protected override void SetData(ObjectEditData data, Base value)
    {
        data.Base = value;
    }
}

public class ObjectSetDock(GameObject target, SystemObjectList list, DockAction old, DockAction updated)
    : SysObjectModification<DockAction>(target, list, old, updated, "SetDock")
{
    protected override void SetData(ObjectEditData data, DockAction value)
    {
        data.Dock = value;
    }
}

public class ObjectSetParent(GameObject target, SystemObjectList list, string old, string updated)
    : SysObjectModification<string>(target, list, old, updated, "SetParent")
{
    protected override void SetData(ObjectEditData data, string value)
    {
        data.ParentObject = value;
    }
}


public class ObjectSetComment(GameObject target, SystemObjectList list, string old, string updated)
    : SysObjectModification<string>(target, list, old, updated, "SetComment")
{
    protected override void SetData(ObjectEditData data, string value)
    {
        data.Comment = value;
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
        target.UpdateDirty();
        tab.ObjectsList.CheckDirty();
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
        if (!obj.TryGetComponent<ObjectEditData>(out var ed) ||
            !ed.IsNewObject) {
            tab.ObjectsList.DeletedObjects.Add(obj.SystemObject);
        }
        tab.OnRemoved(obj);
    }

    public override void Undo()
    {
        obj.Register(tab.World.Physics);
        tab.World.AddObject(obj);
        if (tab.ObjectsList.DeletedObjects.Contains(obj.SystemObject))
            tab.ObjectsList.DeletedObjects.Remove(obj.SystemObject);
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
        tab.World.InitObject(Object, false, systemObject, tab.Data.Resources, null, false);
        Object.GetEditData().IsNewObject = true;
        tab.ObjectsList.Refresh();
        tab.ObjectsList.CheckDirty();
    }

    public override void Undo()
    {
        var n = Object.Nickname;
        tab.World.RemoveObject(Object);
        Object.ClearAll(tab.World.Physics);
        Object.Nickname = n;
        tab.OnRemoved(Object);
        tab.ObjectsList.Refresh();
        tab.ObjectsList.CheckDirty();
    }

    public override string ToString() => $"CreateObject: {systemObject.Nickname}, {systemObject.Archetype.Nickname}";
}
