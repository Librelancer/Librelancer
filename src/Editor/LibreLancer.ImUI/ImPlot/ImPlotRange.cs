using System.Runtime.InteropServices;

namespace LibreLancer.ImUI.ImPlot;

[StructLayout(LayoutKind.Sequential)]
public struct ImPlotRange
{
    public double Min;
    public double Max;
}
