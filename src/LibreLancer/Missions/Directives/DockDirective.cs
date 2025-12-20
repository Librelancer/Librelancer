using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class DockDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.Dock;

    public string Target = "";
    public string Towards = "";

    public DockDirective()
    {
    }

    public DockDirective(Entry entry)
    {
        Target = entry[0].ToString();
        if(entry.Count > 1)
            Towards = entry[1].ToString();
    }

    public DockDirective(PacketReader reader)
    {
        Target = reader.GetString();
        Towards = reader.GetString();
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.Dock);
        writer.Put(Target);
        writer.Put(Towards);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        if (string.IsNullOrWhiteSpace(Towards))
            section.Entry("Dock", Target);
        else
            section.Entry("Dock", Target, Towards);
    }
}
