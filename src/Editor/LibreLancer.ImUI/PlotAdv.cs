using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;

namespace LibreLancer.ImUI;

public static unsafe class PlotAdv
{
    [DllImport("cimgui")]
    static extern int igExtPlot(
        int plotType,
        byte* label,
        delegate* unmanaged<IntPtr, int, float> values_getter,
        delegate* unmanaged<IntPtr, int, IntPtr, int> get_tooltip,
        IntPtr data,
        int values_count,
        int values_offset,
        byte* overlay_text,
        float scale_min,
        float scale_max,
        float size_x,
        float size_y);

    [UnmanagedCallersOnly]
    static float GetValue(IntPtr data, int index) => ((float*)data)[index];

    delegate int NativeTooltipFunc(IntPtr data, int index, IntPtr buffer);

    public static void PlotLines(string label, ReadOnlySpan<float> values, Func<int, float, string> get_tooltip,
        string overlay_text, float scaleMin, float scaleMax, Vector2 size)
    {
        byte* labelBuf = stackalloc byte[256];
        byte* overlayBuf = stackalloc byte[256];
        using var utf8z_label = new ImGuiNET.UTF8ZHelper(labelBuf, 256, label);
        using var utf8z_overlay = new ImGuiNET.UTF8ZHelper(overlayBuf, 256, overlay_text);
        //Tooltip function
        delegate* unmanaged<IntPtr, int, IntPtr, int> tooltipFunc = null;
        NativeTooltipFunc toNative = null;
        if (get_tooltip != null)
        {
            toNative = (data,  index, buffer) =>
            {
                var str = get_tooltip(index, ((float*)data)[index]);
                if (str == null)
                    return 0;
                var bytes = Encoding.UTF8.GetBytes(str);
                if (bytes.Length > 2048) {
                    Marshal.Copy(bytes, 0, buffer, 2047);
                    ((byte*)buffer)[2047] = 0;
                }
                else {
                    Marshal.Copy(bytes, 0, buffer, bytes.Length);
                    ((byte*)buffer)[bytes.Length] = 0;
                }
                return 1;
            };
            tooltipFunc =
                (delegate* unmanaged<IntPtr, int, IntPtr, int>)Marshal.GetFunctionPointerForDelegate(toNative);
        }
        fixed (float* ptr = &values.GetPinnableReference())
        {
            igExtPlot(0, utf8z_label.Pointer, &GetValue, tooltipFunc, (IntPtr)ptr, values.Length,
                0, utf8z_overlay.Pointer, scaleMin, scaleMax, size.X, size.Y);
        }
    }
}
