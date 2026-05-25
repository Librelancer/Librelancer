// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Navmap : UiWidget
    {
        private class DrawObject
        {
            public UiRenderable? Renderable;
            public string? Name;
            public Vector2 XZ;
            public float SolarRadius;
            public int LabelPriority;
            public uint Hash;
        }

        private List<DrawObject> objects = [];

        private class DrawZone
        {
            public Zone Zone;
            public Color4 Tint;
            public string Texture;
            public float Sort;

            public DrawZone(Zone zone, Color4 tint, string texture, float sort)
            {
                Zone = zone;
                Tint = tint;
                Texture = texture;
                Sort = sort;
            }
        }

        private List<DrawZone> zones = [];

        private struct Tradelanes
        {
            public Vector2 StartXZ;
            public Vector2 EndXZ;
        }

        private List<Tradelanes> tradelanes = [];
        private float navmapscale;
        private const float GridSizeDefault = 240000;
        private const float MinimumObjectIconSize = 8f;
        private const float LabelCollisionPadding = 2f;
        private const float SelectorSize = 14f;
        private const float ZoomButtonOffset = 16f;
        private const float AddWaypointButtonOffset = 16f;
        private const float SelectorMenuClosePadding = 8f;
        private const float ZoomedScale = 4f;
        private const float ZoomAnimationDuration = 1.5f;
        private const float DragStartDelay = 0.5f;
        private const float DragStartDistance = 3f;
        private const string SelectSound = "ui_item_select";
        private string systemName = "";

        private readonly Button selectorButton = new();
        private readonly Button zoomInButton = new();
        private readonly Button zoomOutButton = new();
        private readonly Button addWaypointButton = new();
        private readonly List<(DrawObject Object, RectangleF Bounds)> labelCandidates = [];
        private readonly List<RectangleF> placedLabels = [];
        private Vector2? selectorMapPosition;
        private Action<Vector3>? addWaypoint;
        private bool zoomed;
        private float targetZoom = 1f;
        private Vector2 targetOffset;
        private float startZoom = 1f;
        private Vector2 startOffset;
        private float zoomAnimationTime = ZoomAnimationDuration;
        private bool mouseDownOnMap;
        private bool draggingMap;
        private Vector2 mouseDownPosition;
        private Vector2 lastMousePosition;
        private double mouseDownTime;

        public bool LetterMargin { get; set; } = false;

        public bool MapBorder { get; set; } = false;

        [WattleScriptHidden] public NavmapStyle? Style;

        private VertexBuffer vbo = null!;

        private struct ZoneVertex : IVertexType
        {
            public VertexDeclaration GetVertexDeclaration() => new
                (8, new VertexElement(VertexSlots.Position, 2, VertexElementType.Float, false, 0));
        }

        private Func<uint, bool> isVisited = _ => true;

        public void SetVisitFunction(Func<uint, bool> isVisited)
        {
            this.isVisited = isVisited;
        }

        [WattleScriptHidden]
        public void SetAddWaypointFunction(Action<Vector3>? addWaypoint)
        {
            this.addWaypoint = addWaypoint;
        }

        public override void ApplyStylesheet(Stylesheet stylesheet)
        {
            base.ApplyStylesheet(stylesheet);
            Style = stylesheet.Lookup<NavmapStyle>(null);
            selectorButton.SetStyle(stylesheet.Lookup<ButtonStyle>("nav_selector"));
            zoomInButton.SetStyle(Style?.ZoomInButton);
            zoomOutButton.SetStyle(Style?.ZoomOutButton);
            addWaypointButton.SetStyle(Style?.AddWaypointButton);
        }

        public void PopulateIcons(UiContext ctx, StarSystem sys)
        {
            foreach (var l in ctx.Data.NavmapIcons.Libraries())
                ctx.Data.ResourceManager.LoadResourceFile(ctx.Data.DataPath + l);
            objects = [];
            tradelanes = [];
            navmapscale = sys.NavMapScale;

            foreach (var obj in sys.Objects)
            {
                if (obj.Dock is { Kind: DockKinds.Tradelane })
                {
                    if (!string.IsNullOrEmpty(obj.Dock.Target) &&
                        string.IsNullOrEmpty(obj.Dock.TargetLeft))
                    {
                        var start = obj;
                        var end = obj;

                        while (!string.IsNullOrEmpty(end.Dock?.Target))
                        {
                            SystemObject? e = null;
                            foreach (var candidate in sys.Objects)
                            {
                                if (candidate.Nickname.Equals(end.Dock.Target))
                                {
                                    e = candidate;
                                    break;
                                }
                            }

                            if (e == null)
                            {
                                break;
                            }

                            end = e;
                        }

                        if (start != end)
                        {
                            tradelanes.Add(new Tradelanes()
                            {
                                StartXZ = new Vector2(start.Position.X, start.Position.Z),
                                EndXZ = new Vector2(end.Position.X, end.Position.Z)
                            });
                        }
                    }
                }

                if ((obj.Visit & VisitFlags.Hidden) == VisitFlags.Hidden ||
                    obj.Archetype is not { SolarRadius: > 0 } archetype)
                {
                    continue;
                }

                UiRenderable? renderable = null;
                renderable = ctx.Data.NavmapIcons.GetSystemObject(archetype.NavmapIcon);

                var nm = ctx.Data.Infocards?.GetStringResource(obj.IdsName);
                if (string.IsNullOrWhiteSpace(nm))
                    nm = obj.Nickname;

                objects.Add(new DrawObject()
                {
                    Renderable = renderable,
                    Name = nm,
                    XZ = new Vector2(obj.Position.X, obj.Position.Z),
                    SolarRadius = archetype.SolarRadius,
                    LabelPriority = LabelPriority(archetype.Type),
                    Hash = FLHash.CreateID(obj.Nickname)
                });
            }

            zones = [];

            foreach (var zone in sys.Zones)
            {
                if ((zone.VisitFlags & VisitFlags.Hidden) == VisitFlags.Hidden ||
                    (zone.Shape != ShapeKind.Sphere && zone.Shape != ShapeKind.Ellipsoid))
                    continue;
                var tint = zone.PropertyFogColor;
                string? tex = null;
                if ((zone.PropertyFlags & ZonePropFlags.Badlands) == ZonePropFlags.Badlands)
                    tex = "nav_terrain_badlands";
                if ((zone.PropertyFlags & ZonePropFlags.Crystal) == ZonePropFlags.Crystal ||
                    (zone.PropertyFlags & ZonePropFlags.Ice) == ZonePropFlags.Ice)
                    tex = "nav_terrain_ice";
                if ((zone.PropertyFlags & ZonePropFlags.Lava) == ZonePropFlags.Lava)
                    tex = "nav_terrain_lava";
                if ((zone.PropertyFlags & ZonePropFlags.Mines) == ZonePropFlags.Mines)
                    tex = "nav_terrain_mines";
                if ((zone.PropertyFlags & ZonePropFlags.Debris) == ZonePropFlags.Debris)
                    tex = "nav_terrain_debris";
                if ((zone.PropertyFlags & ZonePropFlags.Nomad) == ZonePropFlags.Nomad)
                    tex = "nav_terrain_nomadast";
                if ((zone.PropertyFlags & ZonePropFlags.Rock) == ZonePropFlags.Rock)
                    tex = "asteroidtest";
                if ((zone.PropertyFlags & ZonePropFlags.Cloud) == ZonePropFlags.Cloud)
                    tex = "dustcloud";

                if ((zone.PropertyFlags & ZonePropFlags.Exclusion1) == ZonePropFlags.Exclusion1 ||
                    (zone.PropertyFlags & ZonePropFlags.Exclusion2) == ZonePropFlags.Exclusion2)
                {
                    tex = "";
                    tint = new Color4(0, 0, 0, 0.6f);
                }

                if (tex == null)
                {
                    continue;
                }

                zones.Add(new DrawZone(zone, tint ?? Color4.White, tex, zone.Sort));
            }

            zones.Sort((x, y) => x.Sort.CompareTo(y.Sort));
            systemName = ctx.Data.Infocards!.GetStringResource(sys.IdsName);
        }

        private static int LabelPriority(ArchetypeType type) => type switch
        {
            ArchetypeType.planet => 90,
            ArchetypeType.station => 80,
            ArchetypeType.jump_gate => 70,
            ArchetypeType.jump_hole or ArchetypeType.jumphole => 60,
            ArchetypeType.docking_ring => 50,
            ArchetypeType.sun => 40,
            _ => 0
        };

        private static readonly string[] GRIDNUMBERS =
        [
            "1", "2", "3", "4", "5", "6", "7", "8"
        ];

        private readonly string[] GRIDLETTERS =
        [
            "A", "B", "C", "D", "E", "F", "G", "H"
        ];

        private CachedRenderString[] letterCache = new CachedRenderString[16];
        private CachedRenderString? systemNameCache;
        private CachedRenderString[] objectStrings = null!;

        public float Zoom = 1f;
        public float OffsetX = 0f;
        public float OffsetY = 0f;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NavmapParameters
        {
            public Vector4 Rectangle;
            public Vector2 Tiling;
        }

        public override unsafe void Render(UiContext context, DrawList2D drawList, RectangleF parentRectangle)
        {
            var parentRect = GetMyRectangle(context, parentRectangle);
            var gridIdentSize = 13 * (parentRect.Height / 480);
            var gridIdentFont = context.Data.GetFont("$NavMap800");
            var inputRatio = 480 / context.ViewportHeight;
            var lH = context.RenderContext.Renderer2D.LineHeight(gridIdentFont, context.TextSize(gridIdentSize)) *
                inputRatio + 3;
            RectangleF rectNoScale = GetMapRectangle(parentRect, lH);

            UpdateZoomAndDrag(context, rectNoScale);

            var allClip = context.PointsToPixels(parentRect);
            if (!drawList.PushClip(allClip))
                return;

            // Draw Letters
            var rHoriz = rectNoScale.Width / 8;
            var rVert = rectNoScale.Height / 8;
            int jj = 0;

            for (int i = 0; i < 8; i++)
            {
                var renNum = GRIDNUMBERS[i];
                var renLet = GRIDLETTERS[i];
                var hOff = (rHoriz * i);
                RectangleF letterRect = new RectangleF(rectNoScale.X + (hOff * Zoom) - OffsetX,
                    rectNoScale.Y + rectNoScale.Height + 1, rHoriz * Zoom, lH);
                DrawText(context, drawList, ref letterCache[jj++], letterRect, gridIdentSize, gridIdentFont, InterfaceColor.White,
                    new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center, VerticalAlignment.Bottom,
                    false, renLet);
                var vOff = (rVert * i);
                var numRect = new RectangleF(rectNoScale.X - lH, rectNoScale.Y + (vOff * Zoom) - OffsetY, lH,
                    rVert * Zoom);
                DrawText(context, drawList, ref letterCache[jj++], numRect, gridIdentSize, gridIdentFont, InterfaceColor.White,
                    new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center, VerticalAlignment.Center,
                    false, renNum);
            }

            drawList.PopClip();

            var rect = rectNoScale;
            rect.Width *= Zoom;
            rect.Height *= Zoom;

            var scale = new Vector2(GridSizeDefault / (navmapscale == 0 ? 1 : navmapscale));
            var background = context.Data.NavmapIcons.GetBackground();
            background.DrawWithClip(context, drawList,
                new RectangleF(rect.X - OffsetX, rect.Y - OffsetY, rect.Width, rect.Height), rectNoScale);

            // Draw Zones
            Vector2 WorldToMap(Vector2 a)
            {
                var relPos = (a + (scale / 2)) / scale;
                return new Vector2(rect.X, rect.Y) + relPos * new Vector2(rect.Width, rect.Height)
                       - new Vector2(OffsetX, OffsetY);
            }

            // Clip without bleeding
            var zoneclip = context.PointsToPixels(rectNoScale);
            zoneclip.X++;
            zoneclip.Y++;
            zoneclip.Width -= 1;
            zoneclip.Height -= 1;
            if (zoneclip.Width <= 0) zoneclip.Width = 1;
            if (zoneclip.Height <= 0) zoneclip.Height = 1;
            // Draw zones
            if (!drawList.PushClip(zoneclip))
                return;
            drawList.AddCallback(_ =>
            {
                var zoneMat = Matrix4x4.CreateOrthographicOffCenter(0, context.RenderContext.CurrentViewport.Width,
                    context.RenderContext.CurrentViewport.Height, 0, 0, 1);
                var zoneShader = AllShaders.Navmap.Get(0);
                var np = new NavmapParameters()
                {
                    Rectangle = new Vector4(zoneclip.X,
                        context.RenderContext.CurrentViewport.Height - zoneclip.Y - zoneclip.Height,
                        zoneclip.Width, zoneclip.Height),
                    Tiling = new Vector2(8)
                };
                zoneShader.SetUniformBlock(0, ref zoneMat);
                zoneShader.SetUniformBlock(3, ref np);

                foreach (var zone in zones)
                {
                    Texture2D? texture = null;
                    if (!string.IsNullOrEmpty(zone.Texture))
                        texture = (Texture2D?)context.Data.ResourceManager.FindTexture(zone.Texture);
                    context.RenderContext.Textures[0] = texture;
                    context.RenderContext.Samplers[0] =
                        new(context.RenderContext.PreferredFilterLevel, WrapMode.Repeat, WrapMode.Repeat);
                    zoneShader.SetUniformBlock(4, ref zone.Tint);
                    var dim = zone.Zone.Shape == ShapeKind.Sphere
                        ? new Vector2(zone.Zone.Size.X)
                        : new Vector2(zone.Zone.Size.X, zone.Zone.Size.Z);
                    var screenSize = context.PointsToPixelsF(dim / scale * new Vector2(rect.Width, rect.Height));
                    var meshScale = new Vector3(screenSize.X / dim.X, screenSize.Y / dim.Y, 1);
                    var screenPos =
                        context.PointsToPixels(WorldToMap(new Vector2(zone.Zone.Position.X, zone.Zone.Position.Z)));
                    var world = Matrix4x4.CreateScale(meshScale) *
                                Matrix4x4.CreateTranslation(new Vector3(screenPos.X, screenPos.Y, 0));
                    zoneShader.SetUniformBlock(2, ref world);
                    vbo ??= new VertexBuffer(context.RenderContext, typeof(ZoneVertex), 400, true);
                    void* dst = (void*)vbo.BeginStreaming();
                    var td = zone.Zone.TopDownMesh();
                    fixed (Vector2* src = td)
                        Buffer.MemoryCopy(src, dst, 400 * sizeof(Vector2), sizeof(Vector2) * td.Length);
                    vbo.EndStreaming(td.Length);
                    context.RenderContext.Shader = zoneShader;
                    vbo.Draw(PrimitiveTypes.TriangleList, td.Length / 3);
                }
            });

            drawList.PopClip();

            // System Name
            if (!string.IsNullOrWhiteSpace(systemName))
            {
                var sysNameFont = context.Data.GetFont("$NavMap1600");
                var sysNameSize = 16f * (parentRect.Height / 480);
                DrawText(context, drawList, ref systemNameCache, rectNoScale, sysNameSize, sysNameFont, InterfaceColor.White,
                    new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center,
                    VerticalAlignment.Bottom, false, systemName);
            }

            if (!drawList.PushClip(zoneclip))
                return;

            var fontSize = 11f * (parentRect.Height / 480);
            var font = context.Data.GetFont("$NavMap800");
            // Draw Objects
            if ((CachedRenderString[]?)objectStrings == null || objectStrings.Length < objects.Count)
                objectStrings = new CachedRenderString[objects.Count];
            jj = 0;
            labelCandidates.Clear();

            foreach (var obj in objects)
            {
                if (!isVisited(obj.Hash))
                    continue;
                var posAbs = WorldToMap(obj.XZ);
                var szIcon = MathF.Max(
                    (2 * obj.SolarRadius) / scale.Y * rect.Height,
                    MinimumObjectIconSize);
                var originIcon = szIcon / 2;

                if (obj.Renderable != null)
                {
                    var objRect = new RectangleF(posAbs.X - originIcon, posAbs.Y - originIcon, szIcon, szIcon);
                    obj.Renderable.Draw(context, drawList, objRect);
                }

                if (!string.IsNullOrWhiteSpace(obj.Name))
                {
                    var textSize = context.RenderContext.Renderer2D.MeasureString(
                        font,
                        context.TextSize(fontSize),
                        obj.Name);
                    var width = context.PixelsToPoints(textSize.X) + 2;
                    var height = context.PixelsToPoints(textSize.Y);
                    labelCandidates.Add((obj, new RectangleF(
                        posAbs.X - (width / 2),
                        posAbs.Y + originIcon,
                        width,
                        height)));
                }
            }

            placedLabels.Clear();
            labelCandidates.Sort(CompareLabels);
            foreach (var label in labelCandidates)
            {
                var paddedBounds = new RectangleF(
                    label.Bounds.X - LabelCollisionPadding,
                    label.Bounds.Y - LabelCollisionPadding,
                    label.Bounds.Width + (2 * LabelCollisionPadding),
                    label.Bounds.Height + (2 * LabelCollisionPadding));
                if (IntersectsPlacedLabel(paddedBounds))
                    continue;

                placedLabels.Add(paddedBounds);
                DrawText(context, drawList, ref objectStrings[jj++], label.Bounds,
                    fontSize, font, InterfaceColor.White,
                    new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center,
                    VerticalAlignment.Top, false,
                    label.Object.Name!);
            }

            foreach (var tl in tradelanes)
            {
                var posA = context.PointsToPixels(WorldToMap(tl.StartXZ));
                var posB = context.PointsToPixels(WorldToMap(tl.EndXZ));
                drawList.DrawLine(Color4.CornflowerBlue, posA, posB);
            }

            DrawSelectorMenu(context, drawList, rectNoScale);

            drawList.PopClip();

            // Map Border
            if (MapBorder)
            {
                var pRect = context.PointsToPixels(rectNoScale);
                drawList.DrawRectangle(pRect, Color4.White, 1);
            }

        }

        private static int CompareLabels(
            (DrawObject Object, RectangleF Bounds) a,
            (DrawObject Object, RectangleF Bounds) b)
        {
            var priority = b.Object.LabelPriority.CompareTo(a.Object.LabelPriority);
            return priority != 0 ? priority : b.Object.SolarRadius.CompareTo(a.Object.SolarRadius);
        }

        private bool IntersectsPlacedLabel(RectangleF bounds)
        {
            foreach (var placed in placedLabels)
            {
                if (placed.Intersects(bounds))
                    return true;
            }
            return false;
        }

        private RectangleF GetMapRectangle(RectangleF parentRect, float gridIdentLineHeight)
        {
            if (!LetterMargin)
                return parentRect;
            return new RectangleF(parentRect.X + gridIdentLineHeight, parentRect.Y,
                parentRect.Width - (2 * gridIdentLineHeight),
                parentRect.Height - (2 * gridIdentLineHeight));
        }

        private void UpdateZoomAndDrag(UiContext context, RectangleF mapRect)
        {
            var mousePosition = new Vector2(context.MouseX, context.MouseY);
            if (mouseDownOnMap && context.MouseLeftDown)
            {
                var mouseDelta = mousePosition - mouseDownPosition;
                if (zoomed && !draggingMap &&
                    (mouseDelta.Length() >= DragStartDistance ||
                     context.GlobalTime - mouseDownTime >= DragStartDelay))
                {
                    draggingMap = true;
                }

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
            UpdateSelectorMenuHover(context, mapRect);

            if (zoomAnimationTime < ZoomAnimationDuration)
            {
                zoomAnimationTime = MathF.Min(
                    ZoomAnimationDuration,
                    zoomAnimationTime + (float)context.DeltaTime);
                var t = zoomAnimationTime / ZoomAnimationDuration;
                t = t * t * (3 - (2 * t));
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
            new Vector2(mapRect.X, mapRect.Y) + (point * Zoom) - new Vector2(OffsetX, OffsetY);

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

        private void UpdateSelectorMenuHover(UiContext context, RectangleF mapRect)
        {
            if (selectorMapPosition == null || mouseDownOnMap)
                return;

            var selectorRect = Padded(SelectorRectangle(mapRect), SelectorMenuClosePadding);
            var buttonRect = Padded(ZoomButtonRectangle(mapRect), SelectorMenuClosePadding);
            var waypointRect = Padded(AddWaypointButtonRectangle(mapRect), SelectorMenuClosePadding);
            if (!selectorRect.Contains(context.MouseX, context.MouseY) &&
                !buttonRect.Contains(context.MouseX, context.MouseY) &&
                !waypointRect.Contains(context.MouseX, context.MouseY))
            {
                selectorMapPosition = null;
            }
        }

        private RectangleF SelectorRectangle(RectangleF mapRect)
        {
            if (selectorMapPosition is not { } selector)
                return new RectangleF();
            var selectorScreen = MapToScreenPosition(mapRect, selector);
            return new RectangleF(
                selectorScreen.X - (SelectorSize / 2),
                selectorScreen.Y - (SelectorSize / 2),
                SelectorSize,
                SelectorSize);
        }

        private static RectangleF Padded(RectangleF rect, float padding) =>
            new RectangleF(
                rect.X - padding,
                rect.Y - padding,
                rect.Width + (2 * padding),
                rect.Height + (2 * padding));

        private RectangleF ZoomButtonRectangle(RectangleF mapRect)
        {
            if (selectorMapPosition is not { } selector)
                return new RectangleF();
            var selectorScreen = MapToScreenPosition(mapRect, selector);
            var button = ActiveZoomButton;
            var dimensions = button.GetDimensions();
            button.X = selectorScreen.X - mapRect.X - ZoomButtonOffset - (dimensions.X / 2);
            button.Y = selectorScreen.Y - mapRect.Y - (dimensions.Y / 2);
            return new RectangleF(
                mapRect.X + button.X,
                mapRect.Y + button.Y,
                dimensions.X,
                dimensions.Y);
        }

        private RectangleF AddWaypointButtonRectangle(RectangleF mapRect)
        {
            if (selectorMapPosition is not { } selector || addWaypoint == null)
                return new RectangleF();
            var selectorScreen = MapToScreenPosition(mapRect, selector);
            var dimensions = addWaypointButton.GetDimensions();
            addWaypointButton.X = selectorScreen.X - mapRect.X + AddWaypointButtonOffset - (dimensions.X / 2);
            addWaypointButton.Y = selectorScreen.Y - mapRect.Y - (dimensions.Y / 2);
            return new RectangleF(
                mapRect.X + addWaypointButton.X,
                mapRect.Y + addWaypointButton.Y,
                dimensions.X,
                dimensions.Y);
        }

        private Button ActiveZoomButton => zoomed ? zoomOutButton : zoomInButton;

        private void DrawSelectorMenu(UiContext context, DrawList2D drawList, RectangleF mapRect)
        {
            if (selectorMapPosition == null)
                return;

            var selectorRect = SelectorRectangle(mapRect);
            selectorButton.X = selectorRect.X - mapRect.X;
            selectorButton.Y = selectorRect.Y - mapRect.Y;
            selectorButton.Render(context, drawList, mapRect);

            ZoomButtonRectangle(mapRect);
            ActiveZoomButton.Render(context, drawList, mapRect);

            if (addWaypoint != null)
            {
                AddWaypointButtonRectangle(mapRect);
                addWaypointButton.Render(context, drawList, mapRect);
            }
        }

        private Vector3 MapToWorldPosition(RectangleF mapRect, Vector2 mapPosition)
        {
            var scale = GridSizeDefault / (navmapscale == 0 ? 1 : navmapscale);
            var relative = new Vector2(
                MathHelper.Clamp(mapPosition.X / mapRect.Width, 0, 1),
                MathHelper.Clamp(mapPosition.Y / mapRect.Height, 0, 1));
            var worldXZ = (relative * scale) - new Vector2(scale / 2);
            return new Vector3(worldXZ.X, 0, worldXZ.Y);
        }

        private void SetZoom(RectangleF mapRect, bool enabled)
        {
            zoomed = enabled;
            startZoom = Zoom;
            startOffset = new Vector2(OffsetX, OffsetY);
            zoomAnimationTime = 0;
            targetZoom = enabled ? ZoomedScale : 1f;
            if (enabled && selectorMapPosition is { } selector)
            {
                targetOffset = new Vector2(
                    (selector.X * targetZoom) - (mapRect.Width / 2),
                    (selector.Y * targetZoom) - (mapRect.Height / 2));
            }
            else
            {
                targetOffset = Vector2.Zero;
            }
            ClampTargetOffset(mapRect, targetZoom);
        }

        public void ResetView()
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
        }

        public override bool MouseWanted(UiContext context, RectangleF parentRectangle, float x, float y)
        {
            var mapRect = GetMapRectangle(context, parentRectangle);
            return mapRect.Contains(x, y) ||
                   (selectorMapPosition.HasValue &&
                    (ZoomButtonRectangle(mapRect).Contains(x, y) ||
                     AddWaypointButtonRectangle(mapRect).Contains(x, y)));
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            var mapRect = GetMapRectangle(context, parentRectangle);
            var mousePosition = new Vector2(context.MouseX, context.MouseY);
            if (selectorMapPosition.HasValue && ZoomButtonRectangle(mapRect).Contains(context.MouseX, context.MouseY))
            {
                ActiveZoomButton.OnMouseDown(context, mapRect);
                return;
            }

            if (selectorMapPosition.HasValue && AddWaypointButtonRectangle(mapRect).Contains(context.MouseX, context.MouseY))
            {
                addWaypointButton.OnMouseDown(context, mapRect);
                return;
            }

            if (!mapRect.Contains(context.MouseX, context.MouseY))
                return;

            mouseDownOnMap = true;
            draggingMap = false;
            mouseDownPosition = mousePosition;
            lastMousePosition = mousePosition;
            mouseDownTime = context.GlobalTime;
        }

        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            var mapRect = GetMapRectangle(context, parentRectangle);
            if (draggingMap)
                return;

            if (selectorMapPosition.HasValue && ZoomButtonRectangle(mapRect).Contains(context.MouseX, context.MouseY))
            {
                SetZoom(mapRect, !zoomed);
                selectorMapPosition = null;
                return;
            }

            if (selectorMapPosition.HasValue && AddWaypointButtonRectangle(mapRect).Contains(context.MouseX, context.MouseY))
            {
                addWaypoint?.Invoke(MapToWorldPosition(mapRect, selectorMapPosition.Value));
                selectorMapPosition = null;
                return;
            }

            if (!mapRect.Contains(context.MouseX, context.MouseY))
                return;

            selectorMapPosition = ScreenToMapPosition(mapRect, new Vector2(context.MouseX, context.MouseY));
            context.PlaySound(SelectSound);
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            var mapRect = GetMapRectangle(context, parentRectangle);
            zoomInButton.OnMouseUp(context, mapRect);
            zoomOutButton.OnMouseUp(context, mapRect);
            addWaypointButton.OnMouseUp(context, mapRect);
            mouseDownOnMap = false;
            draggingMap = false;
        }

        private RectangleF GetMapRectangle(UiContext context, RectangleF parentRectangle)
        {
            var parentRect = GetMyRectangle(context, parentRectangle);
            var gridIdentSize = 13 * (parentRect.Height / 480);
            var gridIdentFont = context.Data.GetFont("$NavMap800");
            var inputRatio = 480 / context.ViewportHeight;
            var lH = context.RenderContext.Renderer2D.LineHeight(gridIdentFont, context.TextSize(gridIdentSize)) *
                inputRatio + 3;
            return GetMapRectangle(parentRect, lH);
        }

        private RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X, myPos.Y, Width, Height);
            return myRect;
        }

        public override void Dispose()
        {
            vbo?.Dispose();
        }
    }
}
