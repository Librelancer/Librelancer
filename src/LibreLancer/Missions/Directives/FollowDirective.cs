using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class FollowDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.Follow;

    public string Target = "";
    public float Range0;
    public Vector3 Offset;
    public float Range1;

    public FollowDirective()
    {
    }

    public FollowDirective(PacketReader reader)
    {
        Target = reader.GetString();
        Range0 = reader.GetFloat();
        Offset = reader.GetVector3();
        Range1 = reader.GetFloat();
    }

    public FollowDirective(Entry entry)
    {
        Target = entry[0].ToString();
        Range0 = entry[1].ToSingle();
        Offset = new Vector3(entry[2].ToSingle(), entry[3].ToSingle(), entry[4].ToSingle());
        if(entry.Count > 5)
            Range1 = entry[5].ToSingle();
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.Follow);
        writer.Put(Target);
        writer.Put(Range0);
        writer.Put(Offset);
        writer.Put(Range1);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        if (Range1 != 0)
        {
            section.Entry("Follow", Target, Range0, Offset.X, Offset.Y, Offset.Z, Range1);
        }
        else
        {
            section.Entry("Follow", Target, Range0, Offset.X, Offset.Y, Offset.Z);
        }
    }
}
