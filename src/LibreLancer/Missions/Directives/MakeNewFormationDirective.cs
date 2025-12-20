using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class MakeNewFormationDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.MakeNewFormation;

    public string Formation = "fighter_basic";
    public List<string> Ships = new();

    public MakeNewFormationDirective()
    {

    }

    public MakeNewFormationDirective(Entry entry)
    {
        Formation = entry[0].ToString();
        foreach(var s in entry.Skip(1))
            Ships.Add(s.ToString());
    }

    public MakeNewFormationDirective(PacketReader reader)
    {
        Formation = reader.GetString();
        var c = reader.GetVariableUInt32();
        for (int i = 0; i < c; i++)
        {
            Ships.Add(reader.GetString());
        }
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.MakeNewFormation);
        writer.Put(Formation);
        writer.PutVariableUInt32((uint)Ships.Count);
        foreach(var s in Ships)
            writer.Put(s);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        var e = new string[] { Formation };
        section.Entry("MakeNewFormation", e.Concat(Ships));
    }
}
