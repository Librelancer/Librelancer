using System.Runtime.InteropServices;

namespace LibreLancer.ImUI.ImPlot;

[StructLayout(LayoutKind.Sequential)]
public struct ImPlotPoint
{
    public const int Size = 16;

    public double X;
    public double Y;

    public ImPlotPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}
