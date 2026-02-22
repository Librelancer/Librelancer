using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public class CommentNode : Node
{
    public Vector2 Size { get; private set; } = new(100, 100);
    private string previousName;

    static (Vector2 min, Vector2 max) Expand(Vector2 min, Vector2 max, float x, float y)
    {
        return (new Vector2(min.X - x, min.Y - y),
            new Vector2(max.X + x, max.Y + y));
    }

    public override string Name => BlockName;

    public string BlockName = "Comment Node";

    private Vector2? groupSize;
    public void SetGroupSize(Vector2 size)
    {
        Size = size;
        groupSize = size;
    }

    private NodeSuspendState suspend = new();

    public override void Render(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodeLookups lookups)
    {
        const float CommentAlpha = 0.75f;

        bool openRename = false;

        if (groupSize != null)
        {
            NodeEditor.SetGroupSize(Id, groupSize.Value);
            groupSize = null;
        }

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, CommentAlpha);
        NodeEditor.PushStyleColor(StyleColor.NodeBg, new Color4(255, 255, 255, 64));
        NodeEditor.PushStyleColor(StyleColor.NodeBorder, new Color4(255, 255, 255, 64));

        NodeEditor.BeginNode(Id);
        ImGui.PushID(Id);
        ImGui.Text(Name);
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
            ImGui.Text(Name);
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

        if(openRename)
            suspend.FlagSuspend();

        if (!suspend.ShouldSuspend())
            return;

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
            suspend.FlagSuspend();
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
                undoBuffer.Set("Comment", () => ref BlockName, previousName, BlockName);
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

    public override Node Clone(MissionScriptEditorTab sourceTab)
    {
        return new CommentNode()
        {
            BlockName = this.BlockName
        };
    }
}
