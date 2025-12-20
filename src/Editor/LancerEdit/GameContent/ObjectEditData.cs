using System.Linq;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public interface IObjectData
{
    int IdsName { get; }
    int IdsInfo { get;  }
    int IdsLeft { get; }
    int IdsRight { get; }
    VisitFlags Visit { get; }
    Archetype Archetype { get; }
    Sun Star { get; }
    ObjectLoadout Loadout { get; }
    Faction Reputation { get; }
    Base Base { get; }
    DockAction Dock { get;  }
    string Comment { get; }
    string ParentObject { get; }
}

class SystemObjectAccessor : IObjectData
{
    public SystemObjectAccessor(SystemObject obj) => sysobj = obj;
    private SystemObject sysobj;
    public int IdsName => sysobj.IdsName;
    public int IdsInfo => sysobj.IdsInfo;
    public int IdsLeft => sysobj.IdsLeft;
    public int IdsRight => sysobj.IdsRight;
    public VisitFlags Visit => sysobj.Visit;
    public Archetype Archetype => sysobj.Archetype;
    public Sun Star => sysobj.Star;
    public ObjectLoadout Loadout => sysobj.Loadout;
    public Faction Reputation => sysobj.Reputation;
    public Base Base => sysobj.Base;
    public DockAction Dock => sysobj.Dock;
    public string ParentObject => sysobj.Parent;
    public string Comment => sysobj.Comment;
}

public static class GameObjectExtensions
{
    public static IObjectData Content(this GameObject go)
    {
        if (go.TryGetComponent<ObjectEditData>(out var d))
            return d;
        if (go.SystemObject == null) return null;
        return new SystemObjectAccessor(go.SystemObject);
    }

    public static ObjectEditData GetEditData(this GameObject go, bool create = true)
    {
        if (!go.TryGetComponent<ObjectEditData>(out var d))
        {
            if (create)
            {
                d = new ObjectEditData(go);
                go.AddComponent(d);
            }
        }
        return d;
    }

    public static void UpdateDirty(this GameObject go)
    {
        if (go.TryGetComponent<ObjectEditData>(out var ed))
        {
            if (!ed.CheckDirty())
                go.RemoveComponent(ed);
        }
    }
}


public class ObjectEditData : GameComponent, IObjectData
{
    public bool IsNewObject { get; set; }

    public int IdsName { get; set; }
    public int IdsInfo { get; set; }
    public int IdsLeft { get; set; }
    public int IdsRight { get; set; }
    public VisitFlags Visit { get; set; }
    public Archetype Archetype { get; set; }
    public Sun Star { get; set; }
    public ObjectLoadout Loadout { get; set; }
    public Faction Reputation { get; set; }
    public Base Base { get; set; }
    public DockAction Dock { get; set; }

    public string Comment { get; set; }

    private SystemObject sysobj;

    public SystemObject SystemObject => sysobj;

    public string ParentObject { get; set; }

    public ObjectEditData(GameObject parent) : base(parent)
    {
        sysobj = parent.SystemObject;
        IdsName = sysobj.IdsName;
        IdsInfo = sysobj.IdsInfo;
        IdsLeft = sysobj.IdsLeft;
        IdsRight = sysobj.IdsRight;
        Loadout = sysobj.Loadout;
        Archetype = sysobj.Archetype;
        Star = sysobj.Star;
        Reputation = sysobj.Reputation;
        Visit = sysobj.Visit;
        Base = sysobj.Base;
        Dock = sysobj.Dock;
        Comment = sysobj.Comment;
        ParentObject = sysobj.Parent;
    }

    public ObjectEditData MakeCopy()
    {
        var o = (ObjectEditData) MemberwiseClone();
        o.IsNewObject = true;
        o.sysobj = sysobj.Clone();
        if(Parent != null)
            o.Apply();
        o.Parent = null;
        return o;
    }

    static bool ArrayEqual<T>(T[] a, T[] b)
    {
        if (a == b) return true;
        if ((a?.Length ?? -1) != (b?.Length ?? -1)) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (!Equals(a[i], b[i]))
                return false;
        }
        return true;
    }

    public bool CheckDirty()
    {
        return
            sysobj.Nickname != Parent.Nickname ||
            sysobj.Position != Parent.LocalTransform.Position ||
            sysobj.Rotation != Parent.LocalTransform.Orientation ||
            sysobj.IdsName != IdsName ||
            sysobj.IdsInfo != IdsInfo ||
            sysobj.IdsLeft != IdsLeft ||
            sysobj.IdsRight != IdsRight ||
            sysobj.Archetype != Archetype ||
            sysobj.Star != Star ||
            sysobj.Loadout != Loadout ||
            sysobj.Visit != Visit ||
            sysobj.Reputation != Reputation ||
            sysobj.Base != Base ||
            sysobj.Dock != Dock ||
            sysobj.Parent != ParentObject ||
            sysobj.Comment != Comment;
    }

    public void ApplyTransform(Transform3D localTransform)
    {
        sysobj.Position = localTransform.Position;
        sysobj.Rotation = localTransform.Orientation;
    }

    public string GetName(GameDataManager gameData, Vector3 other)
    {
        if (IdsLeft != 0 && IdsRight != 0)
            return new TradelaneName(Parent, IdsLeft, IdsRight).GetName(gameData, other);
        else
            return new ObjectName(IdsName).GetName(gameData, other);
    }

    public void Apply()
    {
        if (Parent != null)
        {
            sysobj.Nickname = Parent.Nickname;
            ApplyTransform(Parent.LocalTransform);
        }

        sysobj.IdsName = IdsName;
        sysobj.IdsInfo = IdsInfo;
        sysobj.IdsLeft = IdsLeft;
        sysobj.IdsRight = IdsRight;
        sysobj.Archetype = Archetype;
        sysobj.Star = Star;
        sysobj.Loadout = Loadout;
        sysobj.Visit = Visit;
        sysobj.Reputation = Reputation;
        sysobj.Base = Base;
        sysobj.Dock = Dock;
        sysobj.Parent = ParentObject;
        sysobj.Comment = Comment;
        if (Parent != null)
        {
            if (IdsLeft != 0 && IdsRight != 0)
                Parent.Name = new TradelaneName(Parent, IdsLeft, IdsRight);
            else
                Parent.Name = new ObjectName(IdsName);
        }
    }
}
