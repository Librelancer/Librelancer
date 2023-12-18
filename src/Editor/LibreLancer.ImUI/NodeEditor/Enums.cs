using System;

namespace LibreLancer.ImUI.NodeEditor;

public enum StyleVar
{
NodePadding,
NodeRounding,
NodeBorderWidth,
HoveredNodeBorderWidth,
SelectedNodeBorderWidth,
PinRounding,
PinBorderWidth,
LinkStrength,
SourceDirection,
TargetDirection,
ScrollDuration,
FlowMarkerDistance,
FlowSpeed,
FlowDuration,
PivotAlignment,
PivotSize,
PivotScale,
PinCorners,
PinRadius,
PinArrowSize,
PinArrowWidth,
GroupRounding,
GroupBorderWidth,
HighlightConnectedLinks,
SnapLinkToPinDir,
HoveredNodeBorderOffset,
SelectedNodeBorderOffset,

Count
}

public enum StyleColor
{
    Bg,
    Grid,
    NodeBg,
    NodeBorder,
    HovNodeBorder,
    SelNodeBorder,
    NodeSelRect,
    NodeSelRectBorder,
    HovLinkBorder,
    SelLinkBorder,
    HighlightLinkBorder,
    LinkSelRect,
    LinkSelRectBorder,
    PinRect,
    PinRectBorder,
    Flow,
    FlowMarker,
    GroupBg,
    GroupBorder,

    Count
}

public enum PinKind
{
Input,
Output,
}

public enum FlowDirection
{
Forward,
Backward
}

public enum CanvasSizeMode
{
FitVerticalView,
FitHorizontalView,
CenterOnly
}

[Flags]
public enum SaveReasonFlags
{
None       = 0x00000000,
Navigation = 0x00000001,
Position   = 0x00000002,
Size       = 0x00000004,
Selection  = 0x00000008,
AddNode    = 0x00000010,
RemoveNode = 0x00000020,
User       = 0x00000040
}
