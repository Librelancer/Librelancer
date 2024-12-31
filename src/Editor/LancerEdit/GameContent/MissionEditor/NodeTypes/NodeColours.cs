using LibreLancer;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public static class NodeColours
{
    public static VertexDiffuse Command => new(0x73, 0x0e, 0x99, 0xFF);
    public static VertexDiffuse CommandList => new(0x73, 0x0e, 0x99, 0xFF);
    public static VertexDiffuse Trigger => new(0xC3, 0xC6, 0xA8, 0xFF);
    public static VertexDiffuse Action => new(0x00, 0x96, 0x5E, 0xFF);
    public static VertexDiffuse Condition => new(0xCE, 0xDC, 0x00, 0xFF);
}
