using System.Runtime.InteropServices;

namespace LibreLancer.ImUI.ImPlot;

[StructLayout(LayoutKind.Sequential)]
public struct ImPlotRect
{
    public ImPlotRange X;
    public ImPlotRange Y;
}
