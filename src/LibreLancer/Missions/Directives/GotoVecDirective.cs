using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.World;

namespace LibreLancer.Missions.Directives;

public class GotoVecDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.GotoVec;

    public Vector3 Target;
    public GotoKind CruiseKind;
    public float Range;
    public bool Unknown;
    public float MaxThrottle;

    public GotoVecDirective()
    {
    }

    public GotoVecDirective(PacketReader reader)
    {
        Target = reader.GetVector3();
        CruiseKind = (GotoKind)reader.GetByte();
        Range = reader.GetFloat();
        Unknown = reader.GetBool();
        MaxThrottle = reader.GetFloat();
    }

    public GotoVecDirective(Entry e)
    {
        CruiseKind = ParseCruiseKind(e[0].ToString());
        Target = new Vector3(e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
        Range = e[4].ToSingle();
        Unknown = e[5].ToBoolean();
        if(e.Count > 6)
            MaxThrottle = e[6].ToSingle();
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.GotoVec);
        writer.Put(Target);
        writer.Put((byte)CruiseKind);
        writer.Put(Range);
        writer.Put(Unknown);
        writer.Put(MaxThrottle);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        if (MaxThrottle != 0)
            section.Entry("GotoVec", CruiseKindString(CruiseKind), Target.X, Target.Y, Target.Z, Range, Unknown, MaxThrottle);
        else
            section.Entry("GotoVec", CruiseKindString(CruiseKind), Target.X, Target.Y, Target.Z, Range, Unknown);
    }
}
