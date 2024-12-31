using ImGuiNET;
using LibreLancer;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public class NodeActPlaySound : BlueprintNode
{
    private const string Name = "Play Sound";
    public Act_PlaySoundEffect Data { get; set; }
    public NodeActPlaySound(ref int id) : base(ref id, Name, NodeColours.Action)
    {
        Data = new Act_PlaySoundEffect();
    }

    public NodeActPlaySound(ref int id, Act_PlaySoundEffect data) : base(ref id, Name, NodeColours.Action)
    {
        Data = data;
    }

    protected override void RenderContent(GameDataContext gameData, MissionScript missionScript)
    {
        ImGui.Text("Play sound");
    }
}
