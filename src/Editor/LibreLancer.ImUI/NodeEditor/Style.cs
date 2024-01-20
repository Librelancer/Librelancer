using System.Numerics;

namespace LibreLancer.ImUI.NodeEditor;

public unsafe struct Style
{
    public Vector4  NodePadding;
    public float   NodeRounding;
    public float   NodeBorderWidth;
    public float   HoveredNodeBorderWidth;
    public float   HoverNodeBorderOffset;
    public float   SelectedNodeBorderWidth;
    public float   SelectedNodeBorderOffset;
    public float   PinRounding;
    public float   PinBorderWidth;
    public float   LinkStrength;
    public Vector2  SourceDirection;
    public Vector2  TargetDirection;
    public float   ScrollDuration;
    public float   FlowMarkerDistance;
    public float   FlowSpeed;
    public float   FlowDuration;
    public Vector4  PivotAlignment;
    public Vector2  PivotSize;
    public Vector2  PivotScale;
    public float   PinCorners;
    public float   PinRadius;
    public float   PinArrowSize;
    public float   PinArrowWidth;
    public float   GroupRounding;
    public float   GroupBorderWidth;
    public float   HighlightConnectedLinks;
    public float   SnapLinkToPinDir; // when true link will start on the line defined by pin direction
    public fixed float  _Colors[(int)StyleColor.Count * 4];
}
