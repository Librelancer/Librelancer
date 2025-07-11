using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups;

public class FloatEditorPopup : PopupWindow
{
    public override string Title { get; set; } = "Float Editor";
    public override Vector2 InitSize => new Vector2(380, 250) * ImGuiHelper.Scale;

    private float[] floats;
    private bool showWarning = false;
    private int floatsPerRow = 4;
    private LUtfNode node;

    public FloatEditorPopup(LUtfNode selectedNode)
    {
        floats = new float[selectedNode.Data.Length / 4];
        for (int i = 0; i < selectedNode.Data.Length / 4; i++)
        {
            floats[i] = BitConverter.ToSingle(selectedNode.Data, i * 4);
        }
        showWarning = selectedNode.Data.Length != (floats.Length * 4);
        node = selectedNode;
    }

    public override void Draw(bool appearing)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(string.Format("Count: {0} ({1} bytes)", floats.Length, floats.Length * 4));
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Floats Per Row:");
        ImGui.SameLine();
        ImGui.PushItemWidth(95 * ImGuiHelper.Scale);
        ImGui.InputInt("##floatsPerRow", ref floatsPerRow);
        ImGui.PopItemWidth();
        if (floatsPerRow < 1)
            floatsPerRow = 1;
        if (showWarning)
        {
            ImGui.TextColored(Color4.Orange ,$"Node size not divisible by 4 ({node.Data.Length}), may not be float data.");
        }
        ImGui.Separator();
        var h = ImGui.GetWindowHeight();
        ImGui.BeginChild("##scroll", new Vector2(0, h - ImGui.GetCursorPosY() - ImGui.GetFrameHeightWithSpacing() - (8 * ImGuiHelper.Scale)));
        if (ImGui.BeginTable("##floats", floatsPerRow))
        {
            for (int i = 0; i < floats.Length; i++)
            {
                if (i % floatsPerRow == 0)
                    ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(-1);
                ImGui.InputFloat("##" + i, ref floats[i], 0, 0);
                ImGui.PopItemWidth();
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
        if (ImGui.Button("+"))
        {
            Array.Resize(ref floats, floats.Length + 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("-") && floats.Length > 1)
        {
            Array.Resize(ref floats, floats.Length - 1);
        }
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(26 * ImGuiHelper.Scale, 2));
        ImGui.SameLine();
        if (ImGui.Button("Ok"))
        {
            node.Data = UnsafeHelpers.CastArray(floats);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
