using System.Numerics;
using System.Collections.Generic;
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
    public string? CruiseSpeedReference;
    public float CruiseSpeedFullDistance;
    public float CruiseSpeedZeroDistance;
    public float CruiseSpeedUnknown;

    private bool HasCruiseSpeedReference => !string.IsNullOrWhiteSpace(CruiseSpeedReference);

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
        if (reader.GetBool())
        {
            CruiseSpeedReference = reader.GetString();
            CruiseSpeedFullDistance = reader.GetFloat();
            CruiseSpeedZeroDistance = reader.GetFloat();
            CruiseSpeedUnknown = reader.GetFloat();
        }
    }

    public GotoVecDirective(Entry e)
    {
        CruiseKind = ParseCruiseKind(e[0].ToString());
        Target = new Vector3(e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
        Range = e[4].ToSingle();
        Unknown = e[5].ToBoolean();
        if(e.Count > 6)
            MaxThrottle = e[6].ToSingle();
        if (e.Count > 7)
            CruiseSpeedReference = e[7].ToString();
        if (e.Count > 8)
            CruiseSpeedFullDistance = e[8].ToSingle();
        if (e.Count > 9)
            CruiseSpeedZeroDistance = e[9].ToSingle();
        if (e.Count > 10)
            CruiseSpeedUnknown = e[10].ToSingle();
    }

    public override void Put(PacketWriter writer)
    {
        var hasCruiseSpeedReference = HasCruiseSpeedReference;
        writer.Put((byte)ObjListCommands.GotoVec);
        writer.Put(Target);
        writer.Put((byte)CruiseKind);
        writer.Put(Range);
        writer.Put(Unknown);
        writer.Put(MaxThrottle);
        writer.Put(hasCruiseSpeedReference);
        if (hasCruiseSpeedReference)
        {
            writer.Put(CruiseSpeedReference);
            writer.Put(CruiseSpeedFullDistance);
            writer.Put(CruiseSpeedZeroDistance);
            writer.Put(CruiseSpeedUnknown);
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        var hasCruiseSpeedReference = HasCruiseSpeedReference;
        var values = new List<ValueBase>
        {
            CruiseKindString(CruiseKind), Target.X, Target.Y, Target.Z, Range, Unknown
        };
        if (MaxThrottle != 0 || hasCruiseSpeedReference)
            values.Add(MaxThrottle);
        if (CruiseSpeedReference is { } cruiseSpeedReference &&
            !string.IsNullOrWhiteSpace(cruiseSpeedReference))
        {
            values.Add(cruiseSpeedReference);
            values.Add(CruiseSpeedFullDistance);
            values.Add(CruiseSpeedZeroDistance);
            values.Add(CruiseSpeedUnknown);
        }
        section.Entry("GotoVec", values.ToArray());
    }
}
