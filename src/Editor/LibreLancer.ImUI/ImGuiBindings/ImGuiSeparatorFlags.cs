using System;

namespace ImGuiNET;

[Flags]
public enum ImGuiSeparatorFlags
{
    None                    = 0,
    Horizontal              = 1 << 0,   // Axis default to current layout type, so generally Horizontal unless e.g. in a menu bar
    Vertical                = 1 << 1,
    SpanAllColumns          = 1 << 2,   // Make separator cover all columns of a legacy Columns() set.
}
