using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class BreakFormationDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.BreakFormation;

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.BreakFormation);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("BreakFormation", "no_params");
    }
}
