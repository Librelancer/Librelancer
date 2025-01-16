using System;

namespace LibreLancer.ImUI.NodeEditor;

public record struct NodeId
{
    private IntPtr value;
    public static implicit operator IntPtr(NodeId n) => n.value;
    public static implicit operator NodeId(IntPtr p) => new () { value = p };
    public static implicit operator NodeId(int i) => new () { value = (IntPtr)i };

    public override string ToString()
    {
        return value.ToString();
    }
}

public record struct LinkId
{
    private IntPtr value;
    public static implicit operator IntPtr(LinkId n) => n.value;
    public static implicit operator LinkId(IntPtr p) => new LinkId() { value = p };
    public static implicit operator LinkId(int i) => new () { value = (IntPtr)i };

    public override string ToString()
    {
        return value.ToString();
    }
}

public record struct PinId
{
    private IntPtr value;
    public static implicit operator IntPtr(PinId n) => n.value;
    public static implicit operator PinId(IntPtr p) => new PinId() { value = p };
    public static implicit operator PinId(int i) => new () { value = (IntPtr)i };

    public override string ToString()
    {
        return value.ToString();
    }
}
