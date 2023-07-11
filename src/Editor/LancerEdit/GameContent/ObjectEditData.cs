
using System.Linq;
using System.Numerics;
using LibreLancer;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.World;

namespace LancerEdit;

public interface IObjectData
{
    int IdsName { get; }
    int[] IdsInfo { get;  }
    int IdsLeft { get; }
    int IdsRight { get; }
    VisitFlags Visit { get; }
    Archetype Archetype { get; }
    ObjectLoadout Loadout { get; }
    Faction Reputation { get; }
    Base Base { get; }
    DockAction Dock { get;  }
}

class SystemObjectAccessor : IObjectData
{
    public SystemObjectAccessor(SystemObject obj) => sysobj = obj;
    private SystemObject sysobj;
    public int IdsName => sysobj.IdsName;
    public int[] IdsInfo => sysobj.IdsInfo;
    public int IdsLeft => sysobj.IdsLeft;
    public int IdsRight => sysobj.IdsRight;
    public VisitFlags Visit => sysobj.Visit;
    public Archetype Archetype => sysobj.Archetype;
    public ObjectLoadout Loadout => sysobj.Loadout;
    public Faction Reputation => sysobj.Reputation;
    public Base Base => sysobj.Base;
    public DockAction Dock => sysobj.Dock;
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
}


public class ObjectEditData : GameComponent, IObjectData
{
    public bool IsNewObject { get; set; }
    
    public int IdsName { get; set; }
    public int[] IdsInfo { get; set; }
    public int IdsLeft { get; set; }
    public int IdsRight { get; set; }
    public VisitFlags Visit { get; set; }
    public Archetype Archetype { get; set; }
    public ObjectLoadout Loadout { get; set; }
    public Faction Reputation { get; set; }
    public Base Base { get; set; }
    public DockAction Dock { get; set; }

    private SystemObject sysobj;

    public SystemObject SystemObject => sysobj;
    
    public ObjectEditData(GameObject parent) : base(parent)
    {
        sysobj = parent.SystemObject;
        IdsName = sysobj.IdsName;
        IdsInfo = sysobj.IdsInfo.ToArray();
        IdsLeft = sysobj.IdsLeft;
        IdsRight = sysobj.IdsRight;
        Loadout = sysobj.Loadout;
        Archetype = sysobj.Archetype;
        Reputation = sysobj.Reputation;
        Visit = sysobj.Visit;
        Base = sysobj.Base;
        Dock = sysobj.Dock;
    }

    public ObjectEditData MakeCopy()
    {
        var o = (ObjectEditData) MemberwiseClone();
        o.IsNewObject = true;
        o.IdsInfo = IdsInfo?.ToArray();
        o.sysobj = sysobj.Clone();
        if(Parent != null)
            o.Apply();
        o.Parent = null;
        return o;
    }

    public void Apply()
    {
        var pos = Vector3.Transform(Vector3.Zero, Parent.LocalTransform);
        var r = Parent.LocalTransform.ExtractRotation();
        var d = MathHelper.QuatError(r, Quaternion.Identity);

        sysobj.Nickname = Parent.Nickname;
        sysobj.Position = pos;
        sysobj.Rotation = d > 0.0001f ? Matrix4x4.CreateFromQuaternion(r) : null;

        sysobj.IdsName = IdsName;
        sysobj.IdsInfo = IdsInfo.ToArray();
        sysobj.IdsLeft = IdsLeft;
        sysobj.IdsRight = IdsRight;
        sysobj.Archetype = Archetype;
        sysobj.Loadout = Loadout;
        sysobj.Visit = Visit;
        sysobj.Reputation = Reputation;
        sysobj.Base = Base;
        sysobj.Dock = Dock;
        
        if (IdsLeft != 0 && IdsRight != 0)
            Parent.Name = new TradelaneName(Parent, IdsLeft, IdsRight);
        else
            Parent.Name = new ObjectName(IdsName);
    }
}