using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public class CommentNode : Node
{
    public Vector2 Size = new(100, 100);
    private string previousName;

    static (Vector2 min, Vector2 max) Expand(Vector2 min, Vector2 max, float x, float y)
    {
        return (new Vector2(min.X - x, min.Y - y),
            new Vector2(max.X + x, max.Y + y));
    }

    public override string Name => BlockName;

    public string BlockName { get; set; } = "Comment Node";

    public override void Render(GameDataContext gameData, PopupManager popup, ref NodeLookups lookups)
    {
        const float CommentAlpha = 0.75f;

        bool openRename = false;

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, CommentAlpha);
        NodeEditor.PushStyleColor(StyleColor.NodeBg, new Color4(255, 255, 255, 64));
        NodeEditor.PushStyleColor(StyleColor.NodeBorder, new Color4(255, 255, 255, 64));
        NodeEditor.BeginNode(Id);
        ImGui.PushID(Id);
        ImGui.TextUnformatted(Name);
        ImGui.SameLine();
        //Cannot use selectable on the title, layout issues with node editor
        if (Controls.SmallButton($"{Icons.Edit}"))
            openRename = true;
        NodeEditor.Group(Size);
        ImGui.PopID();
        NodeEditor.EndNode();
        NodeEditor.PopStyleColor(2);
        ImGui.PopStyleVar();

        if (NodeEditor.BeginGroupHint(Id))
        {
            var min = NodeEditor.GetGroupMin();
            var bgAlpha = ImGui.GetStyle().Alpha;
            ImGui.SetCursorScreenPos(min - new Vector2(-8, ImGui.GetTextLineHeightWithSpacing() + 4));
            ImGui.BeginGroup();
            ImGui.TextUnformatted(Name);
            ImGui.EndGroup();

            var drawList = NodeEditor.GetHintBackgroundDrawList();
            var hintBoundsMin = ImGui.GetItemRectMin();
            var hintBoundsMax = ImGui.GetItemRectMax();

            var (hintFrameBoundsMin, hintFrameBoundsMax) = Expand(hintBoundsMin, hintBoundsMax, 8, 4);
            drawList.AddRectFilled(
                hintBoundsMin,
                hintBoundsMax,
                ImGui.GetColorU32(new Color4(255, 255, 255, (byte)(64 * bgAlpha))),
                4.0f
            );
            drawList.AddRect(
                hintFrameBoundsMin,
                hintFrameBoundsMax,
                ImGui.GetColorU32(new Color4(255, 255, 255, (byte)(128 * bgAlpha))),
                4.0f
            );
        }
        NodeEditor.EndGroupHint();

        //Deferred popup
        NodeEditor.Suspend();
        ImGui.PushID(Id);
        if (openRename)
        {
            previousName = Name;
            ImGui.OpenPopup("Rename");
        }
        bool o = true; //default param
        if (ImGui.BeginPopupModal("Rename", ref o, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Name: ");
            ImGui.SameLine();
            var n = Name;
            ImGui.PushItemWidth(200 * ImGuiHelper.Scale);
            ImGui.InputText("##name", ref n, 255);
            ImGui.PopItemWidth();
            BlockName = n;
            if (ImGui.Button("Ok"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                BlockName = previousName;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        ImGui.PopID();
        NodeEditor.Resume();
    }
}
