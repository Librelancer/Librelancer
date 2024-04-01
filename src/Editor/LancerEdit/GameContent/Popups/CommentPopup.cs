using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class CommentPopup : PopupWindow
{
    public override string Title { get; set; } = "Comment";

    public override Vector2 InitSize => new(400, 300);

    private string commentText;
    private Action<string> onSet;

    public CommentPopup(string initial, Action<string> onSet)
    {
        commentText = initial ?? "";
        this.onSet = onSet;
    }


    public override void Draw()
    {
        ImGui.InputTextMultiline("##comment", ref commentText, 2048,
            new Vector2(
                ImGui.GetWindowWidth() - 4 * ImGui.GetStyle().FramePadding.X - 2 * ImGuiHelper.Scale,
                ImGui.GetWindowHeight() - ImGui.GetFrameHeightWithSpacing() * 3
            ));
        if (ImGui.Button("Ok"))
        {
            onSet(commentText);
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}