using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor;

public class NodePin
{
    public PinId Id { get; }
    public Node OwnerNode { get; }
    public LinkType LinkType { get; }
    public PinKind PinKind { get; }
    public NodeLink Link { get; set; } = null;

    public NodePin(int id, Node owner, LinkType type, PinKind kind)
    {
        Id = id;
        OwnerNode = owner;
        LinkType = type;
        PinKind = kind;
    }

    public bool CanCreateLink(NodePin pin)
    {
        return !(pin == this || OwnerNode == pin.OwnerNode || (LinkType & pin.LinkType) != 0 || PinKind == pin.PinKind);
    }
}
