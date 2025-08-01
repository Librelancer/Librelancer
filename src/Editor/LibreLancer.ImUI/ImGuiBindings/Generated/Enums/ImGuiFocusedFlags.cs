// <auto-generated/>
// ReSharper disable InconsistentNaming
using System;

namespace ImGuiNET;

/// <summary>
/// Flags for ImGui::IsWindowFocused()
/// </summary>
[Flags]
public enum ImGuiFocusedFlags
{
    None = 0,
    /// <summary>
    /// Return true if any children of the window is focused
    /// </summary>
    ChildWindows = 1<<0,
    /// <summary>
    /// Test from root window (top most parent of the current hierarchy)
    /// </summary>
    RootWindow = 1<<1,
    /// <summary>
    /// Return true if any window is focused. Important: If you are trying to tell how to dispatch your low-level inputs, do NOT use this. Use 'io.WantCaptureMouse' instead! Please read the FAQ!
    /// </summary>
    AnyWindow = 1<<2,
    /// <summary>
    /// Do not consider popup hierarchy (do not treat popup emitter as parent of popup) (when used with _ChildWindows or _RootWindow)
    /// </summary>
    NoPopupHierarchy = 1<<3,
    /// <summary>
    /// ImGuiFocusedFlags_DockHierarchy               = 1 &lt;&lt; 4,   // Consider docking hierarchy (treat dockspace host as parent of docked window) (when used with _ChildWindows or _RootWindow)
    /// </summary>
    RootAndChildWindows = RootWindow | ChildWindows
}
