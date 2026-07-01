// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Graphics;

namespace LibreLancer.Interface;

public partial class Navmap
{
    private void LayoutSelectorMenu(UiContext context, double delta,
        bool showAddWaypoint, RectangleF mapRect, Vector2? mapPosition)
    {
        if (mapPosition == null || !AcceptInput)
        {
            selectorMenu.Visible = false;
            return;
        }

        selectorMenu.Visible = true;
        var selectorScreen = MapToScreenPosition(mapRect, mapPosition.Value);
        addWaypointButton.Visible = showAddWaypoint && addWaypoint != null;
        bestPathButton.Visible = showAddWaypoint && bestPath != null;
        addWaypointButton.Anchor = AnchorKind.Center;
        addWaypointButton.X = AddWaypointButtonOffset;
        addWaypointButton.Y = 0;
        bestPathButton.Anchor = AnchorKind.Center;
        bestPathButton.X = 0;
        bestPathButton.Y = BestPathButtonOffset;
        ActiveZoomButton.X = -ZoomButtonOffset;
        ActiveZoomButton.Y = 0;
        ActiveZoomButton.Anchor = AnchorKind.Center;
        selectorButton.Anchor = AnchorKind.Center;
        zoomInButton.Visible = ActiveZoomButton == zoomInButton;
        zoomOutButton.Visible = ActiveZoomButton == zoomOutButton;
        var l = new Layout(new RectangleF(selectorScreen.X, selectorScreen.Y, 0, 0));
        selectorMenu.OnLayout(context, l, delta);
    }

    private void UpdateSectorTransition(UiContext context, double delta)
    {
        var newState = viewState.Update(delta);
        if (newState != null)
            ResetZoomInstant();
        if (newState == SectorViewState.System)
        {
            if (pendingSectorSystem != null)
                PopulateIcons(context, pendingSectorSystem);
            pendingSectorSystem = null;
        }
    }

    private RectangleF GetMapRectangle(RectangleF parentRect, float gridIdentLineHeight)
    {
        if (!LetterMargin)
            return parentRect;
        return new RectangleF(parentRect.X + gridIdentLineHeight, parentRect.Y,
            parentRect.Width - 2 * gridIdentLineHeight,
            parentRect.Height - 2 * gridIdentLineHeight);
    }

    private void UpdateZoomAndDrag(UiContext context)
    {
        var mousePosition = new Vector2(context.MouseX, context.MouseY);
        if (mouseDownOnMap && context.MouseLeftDown)
        {
            var mouseDelta = mousePosition - mouseDownPosition;
            if (zoomed && !draggingMap &&
                (mouseDelta.Length() >= DragStartDistance ||
                 context.GlobalTime - mouseDownTime >= DragStartDelay))
                draggingMap = true;

            if (draggingMap)
            {
                var delta = mousePosition - lastMousePosition;
                OffsetX -= delta.X;
                OffsetY -= delta.Y;
                targetOffset = new Vector2(OffsetX, OffsetY);
            }
        }
        else
        {
            mouseDownOnMap = false;
            draggingMap = false;
        }

        lastMousePosition = mousePosition;
    }

    private void UpdateZoomAnimation(UiContext context, double delta, RectangleF mapRect)
    {
        if (zoomAnimationTime < ZoomAnimationDuration)
        {
            zoomAnimationTime = MathF.Min(ZoomAnimationDuration, zoomAnimationTime + (float)delta);
            var t = zoomAnimationTime / ZoomAnimationDuration;
            t = t * t * (3 - 2 * t);
            Zoom = MathHelper.Lerp(startZoom, targetZoom, t);
            var offset = Vector2.Lerp(startOffset, targetOffset, t);
            OffsetX = offset.X;
            OffsetY = offset.Y;
        }
        else
        {
            Zoom = targetZoom;
            OffsetX = targetOffset.X;
            OffsetY = targetOffset.Y;
        }

        ClampOffset(mapRect);
    }

    private Vector2 ScreenToMapPosition(RectangleF mapRect, Vector2 point) =>
        (point - new Vector2(mapRect.X, mapRect.Y) + new Vector2(OffsetX, OffsetY)) / Zoom;

    private Vector2 MapToScreenPosition(RectangleF mapRect, Vector2 point) =>
        new Vector2(mapRect.X, mapRect.Y) + point * Zoom - new Vector2(OffsetX, OffsetY);

    private void ClampOffset(RectangleF mapRect)
    {
        ClampOffset(mapRect, Zoom);
        ClampTargetOffset(mapRect, targetZoom);
    }

    private void ClampOffset(RectangleF mapRect, float zoom)
    {
        var maxX = MathF.Max(0, mapRect.Width * zoom - mapRect.Width);
        var maxY = MathF.Max(0, mapRect.Height * zoom - mapRect.Height);
        OffsetX = Math.Clamp(OffsetX, 0, maxX);
        OffsetY = Math.Clamp(OffsetY, 0, maxY);
    }

    private void ClampTargetOffset(RectangleF mapRect, float zoom)
    {
        var maxX = MathF.Max(0, mapRect.Width * zoom - mapRect.Width);
        var maxY = MathF.Max(0, mapRect.Height * zoom - mapRect.Height);
        targetOffset.X = Math.Clamp(targetOffset.X, 0, maxX);
        targetOffset.Y = Math.Clamp(targetOffset.Y, 0, maxY);
    }

    private void DrawSelectorMenu(UiContext context, double delta, DrawList2D drawList) =>
        selectorMenu.Render(context, delta, drawList);

    private Vector3 MapToWorldPosition(RectangleF mapRect, Vector2 mapPosition)
    {
        var scale = GridSizeDefault / (navmapscale == 0 ? 1 : navmapscale);
        var relative = new Vector2(
            MathHelper.Clamp(mapPosition.X / mapRect.Width, 0, 1),
            MathHelper.Clamp(mapPosition.Y / mapRect.Height, 0, 1));
        var worldXZ = relative * scale - new Vector2(scale / 2);
        return new Vector3(worldXZ.X, 0, worldXZ.Y);
    }

    private void SetZoom(RectangleF mapRect, bool enabled)
    {
        if (!viewState.Active(SectorViewState.System))
            return;

        zoomed = enabled;
        startZoom = Zoom;
        startOffset = new Vector2(OffsetX, OffsetY);
        zoomAnimationTime = 0;
        targetZoom = enabled ? ZoomedScale : 1f;
        if (enabled && selectorMapPosition is { } selector)
            targetOffset = new Vector2(
                selector.X * targetZoom - mapRect.Width / 2,
                selector.Y * targetZoom - mapRect.Height / 2);
        else
            targetOffset = Vector2.Zero;
        ClampTargetOffset(mapRect, targetZoom);
    }

    public void ShowSectorView()
    {
        if (!viewState.Active(SectorViewState.System))
            return;
        viewState.Switch(SectorViewState.Sector, FadeOutDuration, FadeInDuration);

        selectorMapPosition = null;
        selectedSectorStar = null;
        mouseDownOnMap = false;
        draggingMap = false;
    }

    public void ShowPlayerSystem()
    {
        if (!viewState.Active(SectorViewState.Sector))
            return;
        var playerSystemHash = playerSystemProvider?.Invoke();
        if (playerSystemHash == null || !universeSystems.TryGetValue(playerSystemHash.Value, out var playerSystem))
            return;
        EnterSystemView(playerSystem);
    }

    private void ResetZoomInstant()
    {
        selectorMapPosition = null;
        zoomed = false;
        Zoom = targetZoom = 1f;
        startZoom = 1f;
        OffsetX = OffsetY = 0;
        startOffset = targetOffset = Vector2.Zero;
        zoomAnimationTime = ZoomAnimationDuration;
        mouseDownOnMap = false;
        draggingMap = false;
        zoomInButton.HeldDown = zoomInButton.Dragging = false;
        zoomOutButton.HeldDown = zoomOutButton.Dragging = false;
        addWaypointButton.HeldDown = addWaypointButton.Dragging = false;
        bestPathButton.HeldDown = bestPathButton.Dragging = false;
    }

    public void ResetView()
    {
        viewState.Reset(SectorViewState.System);
        selectedSectorStar = null;
        pendingSectorSystem = null;
        ResetZoomInstant();
    }

    public override bool MouseWanted(UiContext context, float x, float y) =>
        AcceptInput && (ClientRectangle.Contains(x, y) || selectorMenu.MouseWanted(context, x, y));

    public override void OnMouseDown(UiContext context)
    {
        if (!AcceptInput)
            return;
        selectorMenu.OnMouseDown(context);
        if (selectorMenu.MouseWanted(context, context.MouseX, context.MouseY))
            return;
        var mapRect = GetMapRectangle(context);

        if (!viewState.Active(SectorViewState.System) ||
            !mapRect.Contains(context.MouseX, context.MouseY))
            return;

        var mousePosition = new Vector2(context.MouseX, context.MouseY);
        mouseDownOnMap = true;
        draggingMap = false;
        mouseDownPosition = mousePosition;
        lastMousePosition = mousePosition;
        mouseDownTime = context.GlobalTime;
    }

    private void OnZoomIn(UiContext context)
    {
        if (viewState.Active(SectorViewState.Sector) &&
            selectedSectorStar != null)
        {
            context.PlaySound(ZoomInSound);
            EnterSystemView(selectedSectorStar.System);
        }
        else if (viewState.Active(SectorViewState.System))
        {
            context.PlaySound(zoomed ? ZoomOutSound : ZoomInSound);
            var mapRect = GetMapRectangle(context);
            SetZoom(mapRect, !zoomed);
            selectorMapPosition = null;
        }
    }

    private void OnZoomOut(UiContext context)
    {
        if (viewState.Active(SectorViewState.System))
        {
            context.PlaySound(zoomed ? ZoomOutSound : ZoomInSound);
            var mapRect = GetMapRectangle(context);
            SetZoom(mapRect, !zoomed);
            selectorMapPosition = null;
        }
    }

    private void OnAddWaypoint(UiContext context)
    {
        if (currentDisplaySystem == null || selectorMapPosition == null)
            return;
        var mapRect = GetMapRectangle(context);
        addWaypoint?.Invoke(currentDisplaySystem, MapToWorldPosition(mapRect, selectorMapPosition.Value));
        context.PlaySound(SelectAddSound);
        selectorMapPosition = null;
    }

    private void OnBestPath(UiContext context)
    {
        if (currentDisplaySystem == null || selectorMapPosition == null)
            return;
        var mapRect = GetMapRectangle(context);
        if (bestPath?.Invoke(currentDisplaySystem, MapToWorldPosition(mapRect, selectorMapPosition.Value)) == true)
            context.PlaySound(SelectAddSound);
        selectorMapPosition = null;
    }

    private void EnterSystemView(StarSystem system)
    {
        pendingSectorSystem = system;
        selectedSectorStar = null;
        selectorMapPosition = null;
        zoomInButton.HeldDown = zoomInButton.Dragging = false;
        viewState.Switch(SectorViewState.System, FadeOutDuration, FadeInDuration);
    }

    public override void OnMouseClick(UiContext context)
    {
        if (!AcceptInput)
            return;
        var mapRect = GetMapRectangle(context);
        if (!draggingMap)
            selectorMenu.OnMouseClick(context);
        if (selectorMenu.MouseWanted(context, context.MouseX, context.MouseY))
            return;
        if (viewState.Active(SectorViewState.Sector))
        {
            var clickedStar = SectorStarAt(mapRect, new Vector2(context.MouseX, context.MouseY));
            if (clickedStar != null)
            {
                selectedSectorStar = clickedStar;
                selectorMapPosition = SectorPositionToMap(mapRect, clickedStar.Position) -
                                      new Vector2(mapRect.X, mapRect.Y);
                context.PlaySound(SelectSound);
            }
            else
            {
                selectedSectorStar = null;
                selectorMapPosition = null;
            }

            return;
        }

        if (!viewState.Active(SectorViewState.System) || draggingMap || !mapRect.Contains(context.MouseX, context.MouseY))
            return;

        selectorMapPosition = ScreenToMapPosition(mapRect, new Vector2(context.MouseX, context.MouseY));
        context.PlaySound(SelectSound);
    }

    public override void OnMouseUp(UiContext context)
    {
        if (!AcceptInput)
            return;
        selectorMenu.OnMouseUp(context);
        mouseDownOnMap = false;
        draggingMap = false;
    }

    private RectangleF GetMapRectangle(UiContext context)
    {
        var parentRect = ClientRectangle;
        var gridIdentSize = 16.7f * (parentRect.Height / 480);
        var gridIdentFont = context.Data.GetFont("$NavMap800");
        var inputRatio = 480 / context.ViewportHeight;
        var lH = context.RenderContext.Renderer2D.LineHeight(gridIdentFont, context.TextSize(gridIdentSize)) *
            inputRatio + 3;
        return GetMapRectangle(parentRect, lH);
    }
}
