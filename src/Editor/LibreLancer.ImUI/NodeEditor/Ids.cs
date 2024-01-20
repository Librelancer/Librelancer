using System;

namespace LibreLancer.ImUI.NodeEditor;

public struct NodeId
{
    public IntPtr Value;
    public static implicit operator IntPtr(NodeId n) => n.Value;
    public static implicit operator NodeId(IntPtr p) => new () { Value = p };
    public static implicit operator NodeId(int i) => new () { Value = (IntPtr)i };
}

public struct LinkId
{
    public IntPtr Value;
    public static implicit operator IntPtr(LinkId n) => n.Value;
    public static implicit operator LinkId(IntPtr p) => new LinkId() { Value = p };
    public static implicit operator LinkId(int i) => new () { Value = (IntPtr)i };

}

public struct PinId
{
    public IntPtr Value;
    public static implicit operator IntPtr(PinId n) => n.Value;
    public static implicit operator PinId(IntPtr p) => new PinId() { Value = p };
    public static implicit operator PinId(int i) => new () { Value = (IntPtr)i };
}
