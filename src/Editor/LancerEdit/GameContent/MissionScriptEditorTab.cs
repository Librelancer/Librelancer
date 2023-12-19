using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit;
public class MissionScriptEditorTab : GameContentTab
{
    private GameDataContext gameData;
    private MainWindow win;

    private NodeEditorConfig config;
    private NodeEditorContext context;
    public MissionScriptEditorTab(GameDataContext gameData, MainWindow win, string file)
    {
        Title = "Mission Script Editor";
        this.gameData = gameData;
        this.win = win;
        config = new NodeEditorConfig();
        context = new NodeEditorContext(config);
        BlueprintNodeBuilder.LoadTexture();
    }

    void LayoutNode(IEnumerable<string> pinsIn, IEnumerable<string> pinsOut,
        float fixedWidth)
    {
        var iconSize  =24 * ImGuiHelper.Scale;
        float maxIn = 0;
        float maxOut = 0;
        float padding = 15 * ImGuiHelper.Scale;

        foreach (var p in pinsIn)
        {
            var x = ImGui.CalcTextSize(p).X;
            maxIn = Math.Max(x + iconSize + padding, maxIn);
        }
        foreach (var p in pinsOut)
        {
            var x = ImGui.CalcTextSize(p).X;
            maxOut = Math.Max(x + iconSize + padding, maxOut);
        }


        ImGui.BeginTable("##layout", 3, ImGuiTableFlags.PreciseWidths, new Vector2(maxIn + maxOut + fixedWidth + 4 * ImGuiHelper.Scale, 0),
            maxIn + maxOut + fixedWidth);
        ImGui.TableSetupColumn("##in", ImGuiTableColumnFlags.WidthFixed, maxIn);
        ImGui.TableSetupColumn("##fixed", ImGuiTableColumnFlags.WidthFixed, fixedWidth);
        ImGui.TableSetupColumn("##out", ImGuiTableColumnFlags.WidthFixed, maxOut);
        ImGui.TableNextRow();
    }

    void Inputs()
    {
        ImGui.TableSetColumnIndex(0);
    }

    void Fixed()
    {
        ImGui.TableSetColumnIndex(1);
    }
    void Outputs()
    {
        ImGui.TableSetColumnIndex(2);
    }

    void EndNodeLayout()
    {
        ImGui.EndTable();
    }

    private int selectedItem = 0;
    private VectorIcon selectedIcon;
    public override void Draw(double elapsed)
    {
        NodeEditor.SetCurrentEditor(context);
        int uniqueId = 1;
        int pinID = 100;
        NodeEditor.Begin("My Editor", Vector2.Zero);
        using (var nb = BlueprintNodeBuilder.Begin(uniqueId++))
        {
            nb.Header(Color4.Red);
            ImGui.Text("Header Text");
            nb.EndHeader();
            var iconSize  =new Vector2(24 * ImGuiHelper.Scale);
            LayoutNode(new string[] { "input1", "input2" },
                new string[] { "output1", "output2" }, 200);
            Inputs();
            NodeEditor.BeginPin(pinID++, PinKind.Input);
                VectorIcons.Icon(iconSize, VectorIcon.Flow, false);
                ImGui.SameLine();
                ImGui.Text("input1");
            NodeEditor.EndPin();
            NodeEditor.BeginPin(pinID++, PinKind.Input);
                VectorIcons.Icon(iconSize, VectorIcon.Flow, false);
                ImGui.SameLine();
                ImGui.Text("input2");
            NodeEditor.EndPin();
            Outputs();
            NodeEditor.BeginPin(pinID++, PinKind.Output);
                ImGui.Text("output2");
                ImGui.SameLine();
                VectorIcons.Icon(iconSize, VectorIcon.Flow, false);
            NodeEditor.EndPin();
            NodeEditor.BeginPin(pinID++, PinKind.Output);
                ImGui.Text("output2");
                ImGui.SameLine();
                VectorIcons.Icon(iconSize, VectorIcon.Flow, false);
            NodeEditor.EndPin();
            Fixed();
            ImGui.Button("Hello World!");
            ImGui.Text("Some text");
            nb.Combo("Hello", selectedItem, x => selectedItem = x, new string[] { "a", "b", "c", "d" });
            nb.Combo<VectorIcon>("VectorIcon", selectedIcon, x => selectedIcon = x);
            ImGui.Selectable("Hover me");
            if(ImGui.IsItemHovered())
                nb.Tooltip("Tooltip!");
            EndNodeLayout();
        }
        NodeEditor.End();
        NodeEditor.SetCurrentEditor(null);
    }

    public override void Dispose()
    {
        context.Dispose();
        config.Dispose();
        base.Dispose();
    }
}
