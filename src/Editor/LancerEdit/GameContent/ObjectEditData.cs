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
    public int IdsName;
    public int[] IdsInfo;
    public int IdsLeft;
    public int IdsRight;
    public Archetype Archetype;
    public ObjectLoadout Loadout;

    private SystemObject sysobj;
    
    public ObjectEditData(GameObject parent) : base(parent)
    {
        sysobj = parent.SystemObject;
        IdsName = sysobj.IdsName;
        IdsInfo = sysobj.IdsInfo.ToArray();
        IdsLeft = sysobj.IdsLeft;
        IdsRight = sysobj.IdsRight;
        Loadout = sysobj.Loadout;
        Archetype = sysobj.Archetype;
    }

    public void Apply()
    {
        var pos = Vector3.Transform(Vector3.Zero, Parent.LocalTransform);
        var r = Parent.LocalTransform.ExtractRotation();
        var d = MathHelper.QuatError(r, Quaternion.Identity);

        sysobj.Position = pos;
        sysobj.Rotation = d > 0.0001f ? Matrix4x4.CreateFromQuaternion(r) : null;

        sysobj.IdsName = IdsName;
        sysobj.IdsInfo = IdsInfo.ToArray();
        sysobj.IdsLeft = IdsLeft;
        sysobj.IdsRight = IdsRight;
        sysobj.Archetype = Archetype;
        sysobj.Loadout = Loadout;

        if (IdsLeft != 0 && IdsRight != 0)
            Parent.Name = new TradelaneName(Parent, IdsLeft, IdsRight);
        else
            Parent.Name = new ObjectName(IdsName);
    }
}