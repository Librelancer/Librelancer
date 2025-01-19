using LibreLancer;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public static class NodeColours
{
    public static VertexDiffuse Command => new(0x73, 0x0e, 0x99, 0xFF);
    public static VertexDiffuse CommandList => new(0x73, 0x0e, 0x99, 0xFF);
    public static VertexDiffuse Trigger => new(0x4E, 0xBF, 0xFF, 0xFF);
    public static VertexDiffuse Action => new(0xFF, 0x4E, 0x4E, 0xFF);
    public static VertexDiffuse Condition => new(0xCE, 0xDC, 0x00, 0xFF);
}
