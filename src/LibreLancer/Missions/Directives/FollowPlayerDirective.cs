using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class FollowPlayerDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.FollowPlayer;

    public string Formation = "fighter_basic";
    public List<string> Ships = new();

    public FollowPlayerDirective()
    {

    }

    public FollowPlayerDirective(PacketReader reader)
    {
        Formation = reader.GetString();
        var c = reader.GetVariableUInt32();
        for (int i = 0; i < c; i++)
        {
            Ships.Add(reader.GetString());
        }
    }

    public FollowPlayerDirective(Entry entry)
    {
        Formation = entry[0].ToString();
        foreach(var s in entry.Skip(1))
            Ships.Add(s.ToString());
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.FollowPlayer);
        writer.Put(Formation);
        writer.PutVariableUInt32((uint)Ships.Count);
        foreach(var s in Ships)
            writer.Put(s);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        var e = new string[] { Formation };
        section.Entry("FollowPlayer", e.Concat(Ships));
    }
}
