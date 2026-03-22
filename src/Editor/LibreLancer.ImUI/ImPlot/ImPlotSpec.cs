using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.ImUI.ImPlot;

[StructLayout(LayoutKind.Sequential)]
public struct ImPlotSpec
{
    public Vector4 LineColor; // line color (applies to lines, bar edges); IMPLOT_AUTO_COL will use next Colormap color or current item color
    public float LineWeight; // line weight in pixels (applies to lines, bar edges, marker edges)
    public Vector4 FillColor; // fill color (applies to shaded regions, bar faces); IMPLOT_AUTO_COL will use next Colormap color or current item color
    public float FillAlpha; // alpha multiplier (applies to FillColor and MarkerFillColor)
    public ImPlotMarker Marker; // marker type; specify ImPlotMarker_Auto to use the next unused marker
    public float MarkerSize; // size of markers (radius) *in pixels*
    public Vector4 MarkerLineColor; // marker edge color; IMPLOT_AUTO_COL will use LineColor
    public Vector4 MarkerFillColor; // marker face color; IMPLOT_AUTO_COL will use LineColor
    public float Size; // size of error bar whiskers (width or height), and digital bars (height) *in pixels*
    public int Offset; // data index offset
    public int Stride; // data stride in bytes; IMPLOT_AUTO will result in sizeof(T) where T is the type passed to PlotX
    public ImPlotItemFlags Flags;

    [DllImport("cimgui")]
    static extern unsafe void ImPlotSpec_Construct(ImPlotSpec* spec);

    public unsafe ImPlotSpec()
    {
        fixed (ImPlotSpec* spec = &this)
            ImPlotSpec_Construct(spec);
    }
}
