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
        IntPtr label,
        delegate* unmanaged<IntPtr, int, float> values_getter,
        delegate* unmanaged<IntPtr, int, IntPtr, int> get_tooltip,
        IntPtr data,
        int values_count,
        int values_offset,
        IntPtr overlay_text,
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
        //Label
        byte* native_label;
        int label_byteCount = 0;
        if (label != null)
        {
            label_byteCount = Encoding.UTF8.GetByteCount(label);
            if (label_byteCount > Util.StackAllocationSizeLimit)
            {
                native_label = Util.Allocate(label_byteCount + 1);
            }
            else
            {
                byte* native_label_stackBytes = stackalloc byte[label_byteCount + 1];
                native_label = native_label_stackBytes;
            }
            int native_label_offset = Util.GetUtf8(label, native_label, label_byteCount);
            native_label[native_label_offset] = 0;
        }
        else { native_label = null; }
        //Overlay
        byte* native_overlay_text;
        int overlay_text_byteCount = 0;
        if (overlay_text != null)
        {
            overlay_text_byteCount = Encoding.UTF8.GetByteCount(overlay_text);
            if (overlay_text_byteCount > Util.StackAllocationSizeLimit)
            {
                native_overlay_text = Util.Allocate(overlay_text_byteCount + 1);
            }
            else
            {
                byte* native_overlay_text_stackBytes = stackalloc byte[overlay_text_byteCount + 1];
                native_overlay_text = native_overlay_text_stackBytes;
            }
            int native_overlay_text_offset = Util.GetUtf8(overlay_text, native_overlay_text, overlay_text_byteCount);
            native_overlay_text[native_overlay_text_offset] = 0;
        }
        else { native_overlay_text = null; }
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
            igExtPlot(0, (IntPtr)native_label, &GetValue, tooltipFunc, (IntPtr)ptr, values.Length,
                0, (IntPtr)native_overlay_text, scaleMin, scaleMax, size.X, size.Y);
        }
        //Free
        if (label_byteCount > Util.StackAllocationSizeLimit)
        {
            Util.Free(native_label);
        }
        if (overlay_text_byteCount > Util.StackAllocationSizeLimit)
        {
            Util.Free(native_overlay_text);
        }

    }
}
