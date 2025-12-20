using System;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class SetPriorityDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.SetPriority;

    public bool AlwaysExecute;

    public SetPriorityDirective()
    {
    }

    public SetPriorityDirective(PacketReader reader)
    {
        AlwaysExecute = reader.GetBool();
    }

    public SetPriorityDirective(Entry entry)
    {
        var v = entry[0].ToString();
        if ("ALWAYS_EXECUTE".Equals(v, StringComparison.OrdinalIgnoreCase))
        {
            AlwaysExecute = true;
        }
        else if (!"NORMAL".Equals(v, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException();
        }
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.SetPriority);
        writer.Put(AlwaysExecute);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("SetPriority", AlwaysExecute ? "ALWAYS_EXECUTE" : "NORMAL");
    }
}
