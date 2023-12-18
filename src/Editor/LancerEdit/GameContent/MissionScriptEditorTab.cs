using System.Numerics;
using ImGuiNET;
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
    }

    public override void Draw(double elapsed)
    {
        NodeEditor.SetCurrentEditor(context);
        int uniqueId = 1;
        NodeEditor.Begin("My Editor", Vector2.Zero);
        NodeEditor.BeginNode(uniqueId++);
        ImGui.Text("Node A");
        NodeEditor.BeginPin(uniqueId++, PinKind.Input);
        ImGui.Text("-> In");
        NodeEditor.EndPin();
        ImGui.SameLine();
        NodeEditor.BeginPin(uniqueId++, PinKind.Output);
        ImGui.Text("Out ->");
        NodeEditor.EndPin();
        NodeEditor.EndNode();
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
