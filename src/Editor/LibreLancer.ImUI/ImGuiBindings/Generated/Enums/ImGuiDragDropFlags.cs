// <auto-generated/>
// ReSharper disable InconsistentNaming
using System;

namespace ImGuiNET;

/// <summary>
/// Flags for ImGui::BeginDragDropSource(), ImGui::AcceptDragDropPayload()
/// </summary>
[Flags]
public enum ImGuiDragDropFlags
{
    None = 0,
    /// <summary>
    /// <para>BeginDragDropSource() flags</para>
    /// Disable preview tooltip. By default, a successful call to BeginDragDropSource opens a tooltip so you can display a preview or description of the source contents. This flag disables this behavior.
    /// </summary>
    SourceNoPreviewTooltip = 1<<0,
    /// <summary>
    /// By default, when dragging we clear data so that IsItemHovered() will return false, to avoid subsequent user code submitting tooltips. This flag disables this behavior so you can still call IsItemHovered() on the source item.
    /// </summary>
    SourceNoDisableHover = 1<<1,
    /// <summary>
    /// Disable the behavior that allows to open tree nodes and collapsing header by holding over them while dragging a source item.
    /// </summary>
    SourceNoHoldToOpenOthers = 1<<2,
    /// <summary>
    /// Allow items such as Text(), Image() that have no unique identifier to be used as drag source, by manufacturing a temporary identifier based on their window-relative position. This is extremely unusual within the dear imgui ecosystem and so we made it explicit.
    /// </summary>
    SourceAllowNullID = 1<<3,
    /// <summary>
    /// External source (from outside of dear imgui), won't attempt to read current item/window info. Will always return true. Only one Extern source can be active simultaneously.
    /// </summary>
    SourceExtern = 1<<4,
    /// <summary>
    /// Automatically expire the payload if the source cease to be submitted (otherwise payloads are persisting while being dragged)
    /// </summary>
    PayloadAutoExpire = 1<<5,
    /// <summary>
    /// Hint to specify that the payload may not be copied outside current dear imgui context.
    /// </summary>
    PayloadNoCrossContext = 1<<6,
    /// <summary>
    /// Hint to specify that the payload may not be copied outside current process.
    /// </summary>
    PayloadNoCrossProcess = 1<<7,
    /// <summary>
    /// <para>AcceptDragDropPayload() flags</para>
    /// AcceptDragDropPayload() will returns true even before the mouse button is released. You can then call IsDelivery() to test if the payload needs to be delivered.
    /// </summary>
    AcceptBeforeDelivery = 1<<10,
    /// <summary>
    /// Do not draw the default highlight rectangle when hovering over target.
    /// </summary>
    AcceptNoDrawDefaultRect = 1<<11,
    /// <summary>
    /// Request hiding the BeginDragDropSource tooltip from the BeginDragDropTarget site.
    /// </summary>
    AcceptNoPreviewTooltip = 1<<12,
    /// <summary>
    /// For peeking ahead and inspecting the payload before delivery.
    /// </summary>
    AcceptPeekOnly = AcceptBeforeDelivery | AcceptNoDrawDefaultRect
}
