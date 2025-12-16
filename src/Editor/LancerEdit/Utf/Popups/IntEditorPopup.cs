using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups;

public class IntEditorPopup : PopupWindow
{
    public override string Title { get; set; } = "Int Editor";
    public override Vector2 InitSize => new Vector2(380, 250) * ImGuiHelper.Scale;

    private int[] ints;
    private bool showWarning = false;
    private int intsPerRow = 4;
    private LUtfNode node;
    private bool intHex = false;

    public IntEditorPopup(LUtfNode selectedNode)
    {
        ints = new int[selectedNode.Data.Length / 4];
        for (int i = 0; i < selectedNode.Data.Length / 4; i++)
        {
            ints[i] = BitConverter.ToInt32(selectedNode.Data, i * 4);
        }
        showWarning = selectedNode.Data.Length != (ints.Length * 4);
        node = selectedNode;
    }

    public override void Draw(bool appearing)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(string.Format("Count: {0} ({1} bytes)", ints.Length, ints.Length * 4));
        ImGui.SameLine();
        ImGui.Checkbox("Hex", ref intHex);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Ints Per Row:");
        ImGui.SameLine();
        ImGui.PushItemWidth(95 * ImGuiHelper.Scale);
        //ImGuiExt.InputIntExpr("##intsPerRow", ref intsPerRow);
        ImGui.InputInt("##intsPerRow", ref intsPerRow);
        ImGui.PopItemWidth();
        if (intsPerRow < 1)
            intsPerRow = 1;
        if (showWarning)
        {
            ImGui.TextColored(Color4.Orange ,$"Node size not divisible by 4 ({node.Data.Length}), may not be int data.");
        }
        ImGui.Separator();
        var h = ImGui.GetWindowHeight();
        ImGui.BeginChild("##scroll", new Vector2(0, h - ImGui.GetCursorPosY() - ImGui.GetFrameHeightWithSpacing() - (8 * ImGuiHelper.Scale)));
        if (ImGui.BeginTable("##ints", intsPerRow))
        {
            for (int i = 0; i < ints.Length; i++)
            {
                if (i % intsPerRow == 0)
                    ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(-1);
                ImGuiExt.InputIntExpr("##" + i.ToString(), ref ints[i]);
                //ImGui.InputInt("##" + i.ToString(), ref ints[i], 0, 0, intHex ? ImGuiInputTextFlags.CharsHexadecimal : ImGuiInputTextFlags.CharsDecimal);
                ImGui.PopItemWidth();
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
        if (ImGui.Button("+"))
        {
            Array.Resize(ref ints, ints.Length + 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("-") && ints.Length > 1)
        {
            Array.Resize(ref ints, ints.Length - 1);
        }
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(26 * ImGuiHelper.Scale, 2));
        ImGui.SameLine();
        if (ImGui.Button("Ok"))
        {
            node.Data = UnsafeHelpers.CastArray(ints);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
