// <auto-generated/>
// ReSharper disable InconsistentNaming
using System;

namespace ImGuiNET;

/// <summary>
/// Flags for InvisibleButton() [extended in imgui_internal.h]
/// </summary>
[Flags]
public enum ImGuiButtonFlags
{
    None = 0,
    /// <summary>
    /// React on left mouse button (default)
    /// </summary>
    MouseButtonLeft = 1<<0,
    /// <summary>
    /// React on right mouse button
    /// </summary>
    MouseButtonRight = 1<<1,
    /// <summary>
    /// React on center mouse button
    /// </summary>
    MouseButtonMiddle = 1<<2,
    /// <summary>
    /// [Internal]
    /// </summary>
    MouseButtonMask_ = MouseButtonLeft | MouseButtonRight | MouseButtonMiddle,
    /// <summary>
    /// InvisibleButton(): do not disable navigation/tabbing. Otherwise disabled by default.
    /// </summary>
    EnableNav = 1<<3
}
