using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.ImUI.ImPlot;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImPlotStyle
{
        // plot styling
    public Vector2  PlotDefaultSize;         // = 400,300 default size used when public Vector2(0,0) is passed to BeginPlot
    public Vector2  PlotMinSize;             // = 200,150 minimum size plot frame can be when shrunk
    public float   PlotBorderSize;          // = 1,      line thickness of border around plot area
    public float   MinorAlpha;              // = 0.25    alpha multiplier applied to minor axis grid lines
    public Vector2  MajorTickLen;            // = 10,10   major tick lengths for X and Y axes
    public Vector2  MinorTickLen;            // = 5,5     minor tick lengths for X and Y axes
    public Vector2  MajorTickSize;           // = 1,1     line thickness of major ticks
    public Vector2  MinorTickSize;           // = 1,1     line thickness of minor ticks
    public Vector2  MajorGridSize;           // = 1,1     line thickness of major grid lines
    public Vector2  MinorGridSize;           // = 1,1     line thickness of minor grid lines
    // plot padding
    public Vector2  PlotPadding;             // = 10,10   padding between widget frame and plot area, labels, or outside legends (i.e. main padding)
    public Vector2  LabelPadding;            // = 5,5     padding between axes labels, tick labels, and plot edge
    public Vector2  LegendPadding;           // = 10,10   legend padding from plot edges
    public Vector2  LegendInnerPadding;      // = 5,5     legend inner padding from legend edges
    public Vector2  LegendSpacing;           // = 5,0     spacing between legend entries
    public Vector2  MousePosPadding;         // = 10,10   padding between plot edge and interior mouse location text
    public Vector2  AnnotationPadding;       // = 2,2     text padding around annotation labels
    public Vector2  FitPadding;              // = 0,0     additional fit padding as a percentage of the fit extents (e.g. public Vector2(0.1f,0.1f) adds 10% to the fit extents of X and Y)
    public float   DigitalPadding;          // = 20,     digital plot padding from bottom in pixels
    public float   DigitalSpacing;          // = 4,      digital plot spacing gap in pixels
    // style colors
    [System.Runtime.CompilerServices.InlineArray(((int)(ImPlotCol.COUNT)))]
    private struct __inline_Colors
    {
        public Vector4 _0;
    }
    private __inline_Colors __array_Colors;
    public Span<Vector4> Colors => __array_Colors;
    // colormap
    public ImPlotColormap Colormap;         // The current colormap. Set this to either an ImPlotColormap_ enum or an index returned by AddColormap.
    // settings/flags
    public byte    UseLocalTime;            // = false,  axis labels will be formatted for your timezone when ImPlotAxisFlag_Time is enabled
    public byte    UseISO8601;              // = false,  dates will be formatted according to ISO 8601 where applicable (e.g. YYYY-MM-DD, YYYY-MM, --MM-DD, etc.)
    public byte    Use24HourClock;          // = false,  times will be formatted using a 24 hour clock
}
