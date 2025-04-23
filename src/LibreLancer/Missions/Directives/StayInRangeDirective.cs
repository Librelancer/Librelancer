using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class StayInRangeDirective : MissionDirective
{
    public string Object;
    public Vector3 Point;
    public float Range;
    public bool? Unknown;

    public StayInRangeDirective()
    {

    }

    public StayInRangeDirective(PacketReader reader)
    {
        Object = reader.GetString();
        Point = reader.GetVector3();
        Range = reader.GetFloat();
        Unknown = TriValue(reader.GetByte());
    }

    public StayInRangeDirective(Entry entry)
    {
        if (entry.Count > 3)
        {
            Point = new Vector3(entry[0].ToSingle(), entry[1].ToSingle(), entry[2].ToSingle());
            Range = entry[3].ToSingle();
            if (entry.Count > 4)
            {
                Unknown = entry[4].ToBoolean();
            }
        }
        else
        {
            Object = entry[0].ToString();
            Range = entry[1].ToSingle();
            if (entry.Count > 2)
            {
                Unknown = entry[2].ToBoolean();
            }
        }
    }


    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.StayInRange);
        writer.Put(Object);
        writer.Put(Point);
        writer.Put(Range);
        writer.Put(TriValue(Unknown));
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        var vb = new List<ValueBase>();
        if (!string.IsNullOrWhiteSpace(Object))
        {
            vb.Add(new StringValue(Object));
        }
        else
        {
            vb.Add(Point.X);
            vb.Add(Point.Y);
            vb.Add(Point.Z);
        }
        vb.Add(Range);
        if (Unknown != null)
            vb.Add(Unknown.Value);
        section.Entry("StayInRange", vb.ToArray());
    }
}
