using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.World;

namespace LibreLancer.Missions.Directives;

public class GotoSplineDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.GotoSpline;

    public Vector3 PointA;
    public Vector3 PointB;
    public Vector3 PointC;
    public Vector3 PointD;
    public GotoKind CruiseKind;
    public float Range;
    public bool Unknown;
    public float MaxThrottle;

    public GotoSplineDirective()
    {
    }

    public GotoSplineDirective(PacketReader reader)
    {
        PointA = reader.GetVector3();
        PointB = reader.GetVector3();
        PointC = reader.GetVector3();
        PointD = reader.GetVector3();
        CruiseKind = (GotoKind)reader.GetByte();
        Range = reader.GetFloat();
        Unknown = reader.GetBool();
        MaxThrottle = reader.GetFloat();
    }

    public GotoSplineDirective(Entry entry)
    {
        CruiseKind = ParseCruiseKind(entry[0].ToString());
        PointA = new(entry[1].ToSingle(), entry[2].ToSingle(), entry[3].ToSingle());
        PointB = new(entry[4].ToSingle(), entry[5].ToSingle(), entry[6].ToSingle());
        PointC = new(entry[7].ToSingle(), entry[8].ToSingle(), entry[9].ToSingle());
        PointD = new(entry[10].ToSingle(), entry[11].ToSingle(), entry[12].ToSingle());
        Range = entry[13].ToSingle();
        Unknown = entry[14].ToBoolean();
        if(entry.Count > 15)
            MaxThrottle = entry[15].ToSingle();
    }


    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.GotoSpline);
        writer.Put(PointA);
        writer.Put(PointB);
        writer.Put(PointC);
        writer.Put(PointD);
        writer.Put((byte)CruiseKind);
        writer.Put(Range);
        writer.Put(Unknown);
        writer.Put(MaxThrottle);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> v = [
            CruiseKindString(CruiseKind),
            PointA.X, PointA.Y, PointA.Z,
            PointB.X, PointB.Y, PointB.Z,
            PointC.X, PointC.Y, PointC.Z,
            PointD.X, PointD.Y, PointD.Z,
            Range, Unknown
        ];
        if(MaxThrottle != 0)
            v.Add(MaxThrottle);
        section.Entry("GotoSpline", v.ToArray());
    }
}
