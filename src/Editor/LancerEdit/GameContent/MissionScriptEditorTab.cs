using System;
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

    public override void Draw(double elapsed)
    {
        NodeEditor.SetCurrentEditor(context);
        int uniqueId = 1;
        NodeEditor.Begin("My Editor", Vector2.Zero);
        using (var nb = BlueprintNodeBuilder.Begin(uniqueId++))
        {
            nb.Header(Color4.Red);
            ImGui.Text("Header Text");
            nb.EndHeader();
            ImGui.Text("Node Content");
            ImGui.Text("More blahs here");
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
