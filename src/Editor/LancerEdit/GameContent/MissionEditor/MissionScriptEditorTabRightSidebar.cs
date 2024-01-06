using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.MissionEditor;

public sealed partial class MissionScriptEditorTab
{
    private void RenderRightSidebar()
    {
        ImGuiExt.SeparatorText("Ship Manager");
        RenderMissionShipManager();
    }

    private void RenderMissionShipManager()
    {
    }
}
