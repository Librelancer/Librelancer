using LibreLancer;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public abstract class TriggerEntryNode : BlueprintNode
{
    protected TriggerEntryNode(ref int id, VertexDiffuse? color = null) : base(ref id, color)
    {
    }

    public abstract void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder);
}
