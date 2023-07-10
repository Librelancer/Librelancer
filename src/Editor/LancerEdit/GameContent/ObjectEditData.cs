using System.IO.Compression;
using System.Linq;
using System.Numerics;
using LibreLancer;
using LibreLancer.Client.Components;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using SharpDX.Direct2D1;

namespace LancerEdit;

public class ObjectEditData : GameComponent
{
    public bool IsNewObject;
    
    public int IdsName;
    public int[] IdsInfo;
    public int IdsLeft;
    public int IdsRight;
    public VisitFlags Visit;
    public Archetype Archetype;
    public ObjectLoadout Loadout;
    public Faction Reputation;
    public Base Base;
    public DockAction Dock;

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