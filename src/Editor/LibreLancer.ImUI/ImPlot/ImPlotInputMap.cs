using System.Runtime.InteropServices;
using ImGuiNET;

namespace LibreLancer.ImUI.ImPlot;

[StructLayout(LayoutKind.Sequential)]
public struct ImPlotInputMap
{
    public ImGuiMouseButton Pan;           // LMB    enables panning when held,
    public int              PanMod;        // none   optional modifier that must be held for panning/fitting
    public ImGuiMouseButton Fit;           // LMB    initiates fit when double clicked
    public ImGuiMouseButton Select;        // RMB    begins box selection when pressed and confirms selection when released
    public ImGuiMouseButton SelectCancel;  // LMB    cancels active box selection when pressed; cannot be same as Select
    public int              SelectMod;     // none   optional modifier that must be held for box selection
    public int              SelectHorzMod; // Alt    expands active box selection horizontally to plot edge when held
    public int              SelectVertMod; // Shift  expands active box selection vertically to plot edge when held
    public ImGuiMouseButton Menu;          // RMB    opens context menus (if enabled) when clicked
    public int              OverrideMod;   // Ctrl   when held, all input is ignored; used to enable axis/plots as DND sources
    public int              ZoomMod;       // none   optional modifier that must be held for scroll wheel zooming
    public float            ZoomRate;      // 0.1f   zoom rate for scroll (e.g. 0.1f = 10% plot range every scroll click); make negative to invert
}
