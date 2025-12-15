using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
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
    public string PlayerReference;
    public float MinDistance;
    public float MaxDistance;
    public int PlayerDistanceBehavior;

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
        PlayerReference = reader.GetString();
        MinDistance = reader.GetFloat();
        MaxDistance = reader.GetFloat();
        PlayerDistanceBehavior = reader.GetInt();
    }

    public GotoVecDirective(Entry e)
    {
        CruiseKind = ParseCruiseKind(e[0].ToString());
        Target = new Vector3(e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
        Range = e[4].ToSingle();
        Unknown = e[5].ToBoolean();
        if(e.Count > 6)
            MaxThrottle = e[6].ToSingle();
        if(e.Count > 7)
            PlayerReference = e[7].ToString();
        if(e.Count > 8)
            MinDistance = e[8].ToSingle();
        if(e.Count > 9)
            MaxDistance = e[9].ToSingle();
        if(e.Count > 10)
            PlayerDistanceBehavior = e[10].ToInt32();
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.GotoVec);
        writer.Put(Target);
        writer.Put((byte)CruiseKind);
        writer.Put(Range);
        writer.Put(Unknown);
        writer.Put(MaxThrottle);
        writer.Put(PlayerReference ?? "");
        writer.Put(MinDistance);
        writer.Put(MaxDistance);
        writer.Put(PlayerDistanceBehavior);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        if (PlayerReference != null && MinDistance != 0 && MaxDistance != 0)
        {
            if (MaxThrottle != 0)
                section.Entry("GotoVec", CruiseKindString(CruiseKind), Target.X, Target.Y, Target.Z, Range, Unknown, MaxThrottle, PlayerReference, MinDistance, MaxDistance, PlayerDistanceBehavior);
            else
                section.Entry("GotoVec", CruiseKindString(CruiseKind), Target.X, Target.Y, Target.Z, Range, Unknown, PlayerReference, MinDistance, MaxDistance, PlayerDistanceBehavior);
        }
        else
        {
            if (MaxThrottle != 0)
                section.Entry("GotoVec", CruiseKindString(CruiseKind), Target.X, Target.Y, Target.Z, Range, Unknown, MaxThrottle);
            else
                section.Entry("GotoVec", CruiseKindString(CruiseKind), Target.X, Target.Y, Target.Z, Range, Unknown);
        }
    }
}
