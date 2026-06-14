// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

public readonly record struct NavmapWaypoint(Vector3 Position, int Number);

[UiLoadable]
[WattleScriptUserData]
public class Navmap : UiWidget
{
    private const float GridSizeDefault = 240000;
    private const float MinimumObjectIconSize = 8f;
    private const float LabelCollisionPadding = 2f;
    private const float SelectorSize = 14f;
    private const float ZoomButtonOffset = 16f;
    private const float AddWaypointButtonOffset = 16f;
    private const float ZoomedScale = 4f;
    private const float ZoomAnimationDuration = 1.5f;
    private const float FadeOutDuration = 0.35f;
    private const float FadeInDuration = 0.35f;
    private const float DragStartDelay = 0.5f;
    private const float DragStartDistance = 3f;
    private const float SectorGridSize = 16f;
    private const float SectorGridCellCenter = 0.6f;
    private const float SectorStarBorderMargin = 0.1f;
    private const float SectorStarSmallSize = 5.4f;
    private const float SectorStarMediumSize = 7.2f;
    private const float SectorStarLargeSize = 8.0f;
    private const float SectorStarExtraShrinkMaximum = 0.08f;
    private const float SectorConnectionThickness = 4f;
    private const string SelectSound = "ui_item_select";

    private static readonly string[] GRIDNUMBERS =
    [
        "1", "2", "3", "4", "5", "6", "7", "8"
    ];

    private readonly Button addWaypointButton = new();

    private readonly string[] GRIDLETTERS =
    [
        "A", "B", "C", "D", "E", "F", "G", "H"
    ];

    private readonly List<(DrawObject Object, RectangleF Bounds)> labelCandidates = [];
    private readonly List<RectangleF> placedLabels = [];

    private readonly Button selectorButton = new();
    private readonly Dictionary<int, UiRenderable> userWaypointDigits = [];
    private readonly List<NavmapWaypoint> userWaypoints = [];
    private readonly Button zoomInButton = new();
    private readonly Button zoomOutButton = new();
    private Action<Vector3>? addWaypoint;
    private bool draggingMap;

    private Func<uint, bool> isVisited = _ => true;
    private Vector2 lastMousePosition;

    private readonly CachedRenderString[] letterCache = new CachedRenderString[16];
    private bool loadNewSystem = false;
    private bool mouseDownOnMap;
    private Vector2 mouseDownPosition;
    private double mouseDownTime;
    private float navmapscale;

    private NavmapStyle navStyle = new();

    private List<DrawObject> objects = [];
    private CachedRenderString[] objectStrings = null!;
    public float OffsetX;
    public float OffsetY;
    private StarSystem? pendingSectorSystem;
    private Func<Vector3>? playerPositionProvider;
    private UiRenderable? sectorBackground;

    private readonly List<SectorConnection> sectorConnections = [];
    private InterfaceModel? sectorStarModel;

    private readonly List<SectorStar> sectorStars = [];
    private SectorStar? selectedSectorStar;
    private Vector2? selectorMapPosition;

    private readonly Panel selectorMenu = new();
    private Vector2 startOffset;
    private float startZoom = 1f;
    private string systemName = "";
    private CachedRenderString? systemNameCache;
    private Vector2 targetOffset;
    private float targetZoom = 1f;

    private List<Tradelanes> tradelanes = [];
    private UiRenderable? userWaypointDiamond;
    private Action<List<NavmapWaypoint>>? userWaypointProvider;

    private readonly ViewStateMachine<SectorViewState> viewState = new(SectorViewState.System);

    private List<DrawZone> zones = [];

    public float Zoom = 1f;
    private float zoomAnimationTime = ZoomAnimationDuration;
    private bool zoomed;

    public Navmap()
    {
        selectorMenu.Children.Add(addWaypointButton);
        selectorMenu.Children.Add(zoomInButton);
        selectorMenu.Children.Add(zoomOutButton);
        selectorMenu.Children.Add(selectorButton);

        zoomInButton.OnClick(OnZoomIn);
        zoomOutButton.OnClick(OnZoomOut);
        addWaypointButton.OnClick(OnAddWaypoint);
    }

    public bool AcceptInput { get; set; } = true;

    public bool LetterMargin { get; set; } = false;

    public bool MapBorder { get; set; } = false;

    public string ZoomInSound { get; set; } = "hud_zoom_in";
    public string ZoomOutSound { get; set; } = "hud_zoom_out";


    private Button ActiveZoomButton => zoomed ? zoomOutButton : zoomInButton;

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
        addWaypointButton.Anchor = AnchorKind.Center;
        addWaypointButton.X = AddWaypointButtonOffset;
        addWaypointButton.Y = 0;
        ActiveZoomButton.X = -ZoomButtonOffset;
        ActiveZoomButton.Y = 0;
        ActiveZoomButton.Anchor = AnchorKind.Center;
        selectorButton.Anchor = AnchorKind.Center;
        zoomInButton.Visible = ActiveZoomButton == zoomInButton;
        zoomOutButton.Visible = ActiveZoomButton == zoomOutButton;
        var l = new Layout(new RectangleF(selectorScreen.X, selectorScreen.Y, 0, 0));
        selectorMenu.OnLayout(context, l, delta);
    }

    public void SetVisitFunction(Func<uint, bool> isVisited)
    {
        this.isVisited = isVisited;
    }

    [WattleScriptHidden]
    public void SetUniverse(GameItemDb db)
    {
        sectorStars.Clear();
        sectorConnections.Clear();
        var visibleSystems = new Dictionary<string, StarSystem>(StringComparer.OrdinalIgnoreCase);
        foreach (var system in db.Systems)
        {
            if ((system.Visit & VisitFlags.Hidden) == VisitFlags.Hidden)
                continue;
            visibleSystems[system.Nickname] = system;
            sectorStars.Add(CreateStar(system));
        }

        foreach (var c in db.BuildConnections())
        {
            if (!visibleSystems.ContainsKey(c.From.Nickname) ||
                !visibleSystems.ContainsKey(c.To.Nickname))
                continue;
            sectorConnections.Add(new SectorConnection(c.From.UniversePosition, c.To.UniversePosition));
        }
    }

    [WattleScriptHidden]
    public void SetAddWaypointFunction(Action<Vector3>? addWaypoint)
    {
        this.addWaypoint = addWaypoint;
    }

    [WattleScriptHidden]
    public void SetPlayerPositionProvider(Func<Vector3>? playerPositionProvider)
    {
        this.playerPositionProvider = playerPositionProvider;
    }

    [WattleScriptHidden]
    public void SetUserWaypointProvider(Action<List<NavmapWaypoint>>? userWaypointProvider)
    {
        this.userWaypointProvider = userWaypointProvider;
        if (userWaypointProvider == null)
            userWaypoints.Clear();
    }

    protected override ElementStyle OnRestyle(UiContext context)
    {
        navStyle = new StyleResolver()
            .Add(context.Data.Stylesheet?.Styles.DefaultStyle<NavmapStyle>())
            .Add(Style)
            .Add(BackgroundProperty)
            .Add(BorderProperty)
            .Add(WidthProperty)
            .Add(HeightProperty)
            .Create<NavmapStyle>();
        selectorButton.Style = navStyle.SelectorButton;
        zoomInButton.Style = navStyle.ZoomInButton;
        zoomOutButton.Style = navStyle.ZoomOutButton;
        addWaypointButton.Style = navStyle.AddWaypointButton;
        userWaypointDiamond = null;
        userWaypointDigits.Clear();
        return navStyle;
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
                if (!string.IsNullOrEmpty(obj.Dock.Target) &&
                    string.IsNullOrEmpty(obj.Dock.TargetLeft))
                {
                    var start = obj;
                    var end = obj;

                    while (!string.IsNullOrEmpty(end.Dock?.Target))
                    {
                        SystemObject? e = null;
                        foreach (var candidate in sys.Objects)
                            if (candidate.Nickname.Equals(end.Dock.Target))
                            {
                                e = candidate;
                                break;
                            }

                        if (e == null)
                            break;

                        end = e;
                    }

                    if (start != end)
                        tradelanes.Add(new Tradelanes
                        {
                            StartXZ = new Vector2(start.Position.X, start.Position.Z),
                            EndXZ = new Vector2(end.Position.X, end.Position.Z)
                        });
                }

            if ((obj.Visit & VisitFlags.Hidden) == VisitFlags.Hidden ||
                obj.Archetype is not { SolarRadius: > 0 } archetype)
                continue;

            UiRenderable? renderable = null;
            renderable = ctx.Data.NavmapIcons.GetSystemObject(archetype.NavmapIcon);

            var nm = ctx.Data.Infocards?.GetStringResource(obj.IdsName);
            if (string.IsNullOrWhiteSpace(nm))
                nm = obj.Nickname;

            objects.Add(new DrawObject
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
                continue;

            zones.Add(new DrawZone(zone, tint ?? Color4.White, tex, zone.Sort));
        }

        zones.Sort((x, y) => x.Sort.CompareTo(y.Sort));
        systemName = ctx.Data.Infocards!.GetStringResource(sys.IdsName);
    }

    private static int LabelPriority(ArchetypeType type)
    {
        return type switch
        {
            ArchetypeType.planet => 90,
            ArchetypeType.station => 80,
            ArchetypeType.jump_gate => 70,
            ArchetypeType.jump_hole or ArchetypeType.jumphole => 60,
            ArchetypeType.docking_ring => 50,
            ArchetypeType.sun => 40,
            _ => 0
        };
    }

    public override void Update(UiContext context, double delta)
    {
        base.Update(context, delta);
        UpdateSectorTransition(context, delta);
        var parentRect = ClientRectangle;
        var inputRatio = 480 / context.ViewportHeight;
        var gridIdentSize = 16.7f * (parentRect.Height / 480);
        var gridIdentFont = context.Data.GetFont("$NavMap800");
        var lH = context.RenderContext.Renderer2D.LineHeight(gridIdentFont, context.TextSize(gridIdentSize)) *
            inputRatio + 3;
        var rectNoScale = GetMapRectangle(parentRect, lH);
        if (AcceptInput && viewState.Active(SectorViewState.System))
        {
            UpdateZoomAndDrag(context);
        }
        else
        {
            mouseDownOnMap = false;
            draggingMap = false;
            UpdateZoomAnimation(context, delta, rectNoScale);
        }

        var mapRect = GetMapRectangle(context);
        UpdateZoomAnimation(context, delta, mapRect);
        selectorMenu.Update(context, delta);
    }

    public override void OnLayout(UiContext context, Layout layout, double delta)
    {
        base.OnLayout(context, layout, delta);
        var mapRect = GetMapRectangle(context);
        LayoutSelectorMenu(context, delta, viewState.Active(SectorViewState.System),
            mapRect, selectorMapPosition);
    }

    public override unsafe void Render(UiContext context, double delta, DrawList2D drawList)
    {
        var parentRect = ClientRectangle;
        var gridIdentSize = 16.7f * (parentRect.Height / 480);
        var gridIdentFont = context.Data.GetFont("$NavMap800");
        var inputRatio = 480 / context.ViewportHeight;
        var lH = context.RenderContext.Renderer2D.LineHeight(gridIdentFont, context.TextSize(gridIdentSize)) *
            inputRatio + 3;
        var rectNoScale = GetMapRectangle(parentRect, lH);

        var jj = 0;
        var systemAlpha = viewState.Alpha(SectorViewState.System);
        var sectorAlpha = viewState.Alpha(SectorViewState.Sector);
        if (systemAlpha > 0)
        {
            // Draw grid identifiers
            var rHoriz = rectNoScale.Width / 8;
            var rVert = rectNoScale.Height / 8;

            var letterClip = new RectangleF(rectNoScale.X - lH - lH * 0.15f,
                rectNoScale.Y, 3 * lH + lH * 0.15f, rectNoScale.Height);
            if (drawList.PushClip(context.PointsToPixels(letterClip)))
            {
                for (var i = 0; i < 8; i++)
                {
                    var renNum = GRIDNUMBERS[i];
                    var vOff = rVert * i;
                    var numRect = new RectangleF(rectNoScale.X - lH - lH * 0.15f, rectNoScale.Y + vOff * Zoom - OffsetY,
                        lH,
                        rVert * Zoom);
                    RenderText(context, drawList, ref letterCache[jj++], numRect, gridIdentSize, gridIdentFont,
                        context.Data.GetColor("text"),
                        new InterfaceColor { Color = Color4.Black }, HorizontalAlignment.Center,
                        VerticalAlignment.Center,
                        false, renNum, systemAlpha);
                }

                drawList.PopClip();
            }

            var numberClip = new RectangleF(rectNoScale.X, rectNoScale.Y + rectNoScale.Height + 1,
                rectNoScale.Width, lH * 2);
            if (drawList.PushClip(context.PointsToPixels(numberClip)))
            {
                for (var i = 0; i < 8; i++)
                {
                    var renLet = GRIDLETTERS[i];
                    var hOff = rHoriz * i;
                    var letterRect = new RectangleF(rectNoScale.X + hOff * Zoom - OffsetX,
                        rectNoScale.Y + rectNoScale.Height + 1, rHoriz * Zoom, lH);
                    RenderText(context, drawList, ref letterCache[jj++], letterRect, gridIdentSize, gridIdentFont,
                        context.Data.GetColor("text"),
                        new InterfaceColor { Color = Color4.Black }, HorizontalAlignment.Center,
                        VerticalAlignment.Bottom,
                        false, renLet, systemAlpha);
                }

                drawList.PopClip();
            }
        }

        var rect = rectNoScale;
        rect.Width *= Zoom;
        rect.Height *= Zoom;

        var scale = new Vector2(GridSizeDefault / (navmapscale == 0 ? 1 : navmapscale));
        if (systemAlpha > 0)
        {
            var background = context.Data.NavmapIcons.GetBackground();
            background.DrawWithClip(context, drawList,
                new RectangleF(rect.X - OffsetX, rect.Y - OffsetY, rect.Width, rect.Height), rectNoScale, systemAlpha);
        }

        if (sectorAlpha > 0)
        {
            sectorBackground ??= [new DisplayModel(GetResourceModel(context, "nav_prettymap"))];
            sectorBackground.DrawWithClip(context, drawList, rectNoScale, rectNoScale, sectorAlpha);
            DrawSectorConnections(context, drawList, rectNoScale, sectorAlpha);
            DrawSectorStars(context, drawList, rectNoScale, sectorAlpha);
            if (AcceptInput && viewState.Active(SectorViewState.Sector))
                DrawSelectorMenu(context, delta, drawList);
        }

        if (systemAlpha <= 0)
        {
            if (MapBorder)
            {
                var pRect = context.PointsToPixels(rectNoScale);
                drawList.DrawRectangle(pRect, new Color4(1, 1, 1, sectorAlpha), 1);
            }

            return;
        }

        // Draw Zones
        Vector2 WorldToMap(Vector2 a)
        {
            var relPos = (a + scale / 2) / scale;
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
        // Texture coordinate generation origin
        var zoneOrigin =
            context.PointsToPixels(new RectangleF(rect.X - OffsetX, rect.Y - OffsetY, rect.Width, rect.Height));
        // Draw zones
        if (!drawList.PushClip(zoneclip))
            return;
        drawList.AddCallback(_ =>
        {
            var zoneMat = Matrix4x4.CreateOrthographicOffCenter(0, context.RenderContext.CurrentViewport.Width,
                context.RenderContext.CurrentViewport.Height, 0, 0, 1);
            var zoneShader = AllShaders.Navmap.Get(0);
            var np = new NavmapParameters
            {
                Rectangle = new Vector4(zoneOrigin.X,
                    context.RenderContext.CurrentViewport.Height - zoneOrigin.Y - zoneOrigin.Height,
                    zoneOrigin.Width, zoneOrigin.Height),
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
                context.RenderContext.Samplers[0] = new SamplerState(context.RenderContext.PreferredFilterLevel,
                    WrapMode.Repeat, WrapMode.Repeat);
                var tint = zone.Tint;
                tint.A *= systemAlpha;
                zoneShader.SetUniformBlock(4, ref tint);
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
                context.NavmapBuffer ??= new VertexBuffer(context.RenderContext, typeof(ZoneVertex), 400, true);
                var dst = (void*)context.NavmapBuffer.BeginStreaming();
                var td = zone.Zone.TopDownMesh();
                fixed (Vector2* src = td)
                {
                    Buffer.MemoryCopy(src, dst, 400 * sizeof(Vector2), sizeof(Vector2) * td.Length);
                }

                context.NavmapBuffer.EndStreaming(td.Length);
                context.RenderContext.Shader = zoneShader;
                context.NavmapBuffer.Draw(PrimitiveTypes.TriangleList, td.Length / 3);
            }
        });

        drawList.PopClip();

        // System Name
        if (!string.IsNullOrWhiteSpace(systemName))
        {
            var sysNameFont = context.Data.GetFont("$NavMap1600");
            var sysNameSize = 16f * (parentRect.Height / 480);
            RenderText(context, drawList, ref systemNameCache, rectNoScale, sysNameSize, sysNameFont,
                InterfaceColor.White,
                new InterfaceColor { Color = Color4.Black }, HorizontalAlignment.Center,
                VerticalAlignment.Bottom, false, systemName, systemAlpha);
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
                2 * obj.SolarRadius / scale.Y * rect.Height,
                MinimumObjectIconSize);
            var originIcon = szIcon / 2;

            if (obj.Renderable != null)
            {
                var objRect = new RectangleF(posAbs.X - originIcon, posAbs.Y - originIcon, szIcon, szIcon);
                obj.Renderable.Draw(context, drawList, objRect, systemAlpha);
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
                    posAbs.X - width / 2,
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
                label.Bounds.Width + 2 * LabelCollisionPadding,
                label.Bounds.Height + 2 * LabelCollisionPadding);
            if (IntersectsPlacedLabel(paddedBounds))
                continue;

            placedLabels.Add(paddedBounds);
            RenderText(context, drawList, ref objectStrings[jj++], label.Bounds,
                fontSize, font, InterfaceColor.White,
                new InterfaceColor { Color = Color4.Black }, HorizontalAlignment.Center,
                VerticalAlignment.Top, false,
                label.Object.Name!, systemAlpha);
        }

        foreach (var tl in tradelanes)
        {
            var posA = context.PointsToPixels(WorldToMap(tl.StartXZ));
            var posB = context.PointsToPixels(WorldToMap(tl.EndXZ));
            var color = Color4.CornflowerBlue;
            color.A *= systemAlpha;
            drawList.DrawLine(color, posA, posB);
        }

        DrawUserWaypointRoute(context, drawList, WorldToMap, systemAlpha);

        if (AcceptInput && viewState.Active(SectorViewState.System))
            DrawSelectorMenu(context, delta, drawList);

        drawList.PopClip();

        // Map Border
        if (MapBorder)
        {
            var pRect = context.PointsToPixels(rectNoScale);
            drawList.DrawRectangle(pRect, new Color4(1, 1, 1, systemAlpha), 1);
        }
    }

    private void DrawUserWaypointRoute(
        UiContext context,
        DrawList2D drawList,
        Func<Vector2, Vector2> worldToMap,
        float alpha)
    {
        if (userWaypointProvider != null)
        {
            userWaypoints.Clear();
            userWaypointProvider(userWaypoints);
        }

        if (userWaypoints.Count == 0 || alpha <= 0)
            return;

        var lineColor = navStyle.UserWaypointColor.GetColor(context.GlobalTime);
        lineColor.A *= alpha;
        var currentPlayerPosition = playerPositionProvider?.Invoke();
        var routePoints = new Vector2[userWaypoints.Count + (currentPlayerPosition.HasValue ? 1 : 0)];
        var pointIndex = 0;
        if (currentPlayerPosition.HasValue)
            routePoints[pointIndex++] = context.PointsToPixels(worldToMap(
                new Vector2(currentPlayerPosition.Value.X, currentPlayerPosition.Value.Z)));

        for (var i = 0; i < userWaypoints.Count; i++)
            routePoints[pointIndex++] = context.PointsToPixels(worldToMap(
                new Vector2(userWaypoints[i].Position.X, userWaypoints[i].Position.Z)));

        drawList.DrawPolyline(routePoints, (VertexDiffuse)lineColor, navStyle.UserWaypointRouteThickness);

        for (var i = 0; i < userWaypoints.Count; i++)
        {
            var point = worldToMap(new Vector2(userWaypoints[i].Position.X, userWaypoints[i].Position.Z));
            GetUserWaypointDiamond(context)
                .Draw(context, drawList, Centered(point, navStyle.UserWaypointSize, navStyle.UserWaypointSize), alpha);

            if (i == 0 || i == userWaypoints.Count - 1)
                DrawWaypointNumber(context, drawList, point, userWaypoints[i].Number, alpha);
        }
    }

    private static RectangleF Centered(Vector2 center, float width, float height)
    {
        return new RectangleF(center.X - width / 2, center.Y - height / 2, width, height);
    }

    private static Vector2 SectorPositionToMap(RectangleF rect, Vector2 sectorPosition)
    {
        var rel = SectorStarRelativePosition(sectorPosition);
        return new Vector2(
            MathHelper.Lerp(rect.X, rect.X + rect.Width, rel.X),
            MathHelper.Lerp(rect.Y, rect.Y + rect.Height, rel.Y));
    }

    private void DrawSectorConnections(UiContext context, DrawList2D drawList, RectangleF rect, float alpha)
    {
        if (sectorConnections.Count == 0 || alpha <= 0)
            return;

        var lineColor = new Color4(0.000f, 0.255f, 0.506f, alpha);
        foreach (var connection in sectorConnections)
        {
            var start = context.PointsToPixels(SectorPositionToMap(rect, connection.Source));
            var end = context.PointsToPixels(SectorPositionToMap(rect, connection.Target));
            drawList.DrawLine(lineColor, start, end, SectorConnectionThickness);
        }
    }

    private void DrawSectorStars(UiContext context, DrawList2D drawList, RectangleF rect, float alpha)
    {
        if (sectorStars.Count == 0 || alpha <= 0)
            return;

        sectorStarModel ??= GetResourceModel(context, "nav_star");
        foreach (var star in sectorStars)
        {
            if (star.Renderable == null)
                CreateSectorStarRenderable(context, star);
            var center = SectorPositionToMap(rect, star.Position);
            var pulse = 0.5f + 0.5f * MathF.Sin((float)context.GlobalTime * 1.8f + star.Phase);
            var brightness = 0.45f + 0.55f * pulse;
            star.Tint!.Color = SectorStarTint(star.TintOffset, brightness);
            star.Renderable!.Draw(context, drawList, Centered(center, star.Size, star.Size), alpha * brightness);
        }
    }

    private SectorStar? SectorStarAt(RectangleF rect, Vector2 point)
    {
        SectorStar? best = null;
        var bestDistance = float.MaxValue;
        foreach (var star in sectorStars)
        {
            var center = SectorPositionToMap(rect, star.Position);
            var hitSize = MathF.Max(star.Size, SelectorSize);
            if (!Centered(center, hitSize, hitSize).Contains(point.X, point.Y))
                continue;

            var distance = Vector2.DistanceSquared(center, point);
            if (distance < bestDistance)
            {
                best = star;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static Vector2 SectorStarRelativePosition(Vector2 position)
    {
        var x = (position.X + SectorGridCellCenter) / SectorGridSize;
        var y = (position.Y + SectorGridCellCenter) / SectorGridSize;
        return new Vector2(
            MathHelper.Lerp(SectorStarBorderMargin, 1f - SectorStarBorderMargin, MathHelper.Clamp(x, 0f, 1f)),
            MathHelper.Lerp(SectorStarBorderMargin, 1f - SectorStarBorderMargin, MathHelper.Clamp(y, 0f, 1f)));
    }

    private void CreateSectorStarRenderable(UiContext context, SectorStar star)
    {
        sectorStarModel ??= GetResourceModel(context, "nav_star");
        star.Tint = SectorStarTint(star.TintOffset, 1f);
        star.Renderable = [new DisplayModel(sectorStarModel, star.Tint, true)];
    }

    private static Color4 SectorStarTint(float tintOffset, float brightness)
    {
        var warmth = MathHelper.Clamp(tintOffset, -1f, 1f);
        var redAmount = MathF.Max(0f, -warmth);
        var yellowAmount = MathF.Max(0f, warmth);
        var neutral = new Color4(1.00f, 0.96f, 0.90f, 1f);
        var red = new Color4(1.28f, 0.54f, 0.48f, 1f);
        var yellow = new Color4(1.12f, 1.04f, 0.58f, 1f);
        var redMix = MathF.Min(0.72f, redAmount * 0.9f);
        var yellowMix = MathF.Min(0.52f, yellowAmount * 0.7f);
        var baseColor = redAmount > yellowAmount
            ? Color4.Lerp(neutral, red, redMix)
            : Color4.Lerp(neutral, yellow, yellowMix);
        var pulseColor = 0.82f + 0.25f * brightness;
        return new Color4(baseColor.Rgb * pulseColor, 1f);
    }

    private SectorStar CreateStar(StarSystem system)
    {
        var hash = FLHash.CreateID(system.Nickname);
        var size = ((hash >> 16) % 3) switch
        {
            0 => SectorStarSmallSize,
            1 => SectorStarMediumSize,
            _ => SectorStarLargeSize
        };
        if (((hash >> 8) & 0x3) == 0)
            size *= 1f - (hash & 0xFF) / 255f * SectorStarExtraShrinkMaximum;
        return new SectorStar
        {
            Position = system.UniversePosition,
            System = system,
            Phase = (hash & 0xFFFF) / 65535f * MathF.PI * 2f,
            TintOffset = ((hash & 0x7FFF) / 32767f - 0.5f) * 1.15f,
            Size = size
        };
    }

    private void DrawWaypointNumber(
        UiContext context,
        DrawList2D drawList,
        Vector2 center,
        int number,
        float alpha)
    {
        var digits = Math.Max(0, number).ToString();
        var totalWidth = digits.Length * navStyle.UserWaypointDigitWidth;
        var x = center.X - totalWidth / 2;
        var y = center.Y - navStyle.UserWaypointDigitHeight / 2;
        for (var i = 0; i < digits.Length; i++)
        {
            var digit = digits[i] - '0';
            var rect = new RectangleF(
                x + i * navStyle.UserWaypointDigitWidth,
                y,
                navStyle.UserWaypointDigitWidth,
                navStyle.UserWaypointDigitHeight);
            GetUserWaypointDigit(context, digit).Draw(context, drawList, rect, alpha);
        }
    }

    private UiRenderable GetUserWaypointDiamond(UiContext context)
    {
        userWaypointDiamond ??=
        [
            new DisplayModel(
                GetResourceModel(context, "nav_waypointdiamond"),
                navStyle.UserWaypointColor, true)
        ];
        return userWaypointDiamond;
    }

    private UiRenderable GetUserWaypointDigit(UiContext context, int digit)
    {
        if (!userWaypointDigits.TryGetValue(digit, out var renderable))
        {
            renderable =
                [new DisplayModel(GetResourceModel(context, $"waypoint{digit}"), navStyle.UserWaypointDigitColor)];
            userWaypointDigits.Add(digit, renderable);
        }

        return renderable;
    }


    private static InterfaceModel GetResourceModel(UiContext context, string name)
    {
        foreach (var model in context.Data.Resources.Models)
            if (string.Equals(model.Name, name, StringComparison.OrdinalIgnoreCase))
                return model;
        throw new Exception($"Missing interface model resource {name}");
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
            if (placed.Intersects(bounds))
                return true;
        return false;
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
            zoomAnimationTime = MathF.Min(
                ZoomAnimationDuration,
                zoomAnimationTime + (float)delta);
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

    private Vector2 ScreenToMapPosition(RectangleF mapRect, Vector2 point)
    {
        return (point - new Vector2(mapRect.X, mapRect.Y) + new Vector2(OffsetX, OffsetY)) / Zoom;
    }

    private Vector2 MapToScreenPosition(RectangleF mapRect, Vector2 point)
    {
        return new Vector2(mapRect.X, mapRect.Y) + point * Zoom - new Vector2(OffsetX, OffsetY);
    }

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

    private void DrawSelectorMenu(UiContext context, double delta, DrawList2D drawList)
    {
        selectorMenu.Render(context, delta, drawList);
    }

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
    }

    public void ResetView()
    {
        viewState.Reset(SectorViewState.System);
        selectedSectorStar = null;
        pendingSectorSystem = null;
        ResetZoomInstant();
    }

    public override bool MouseWanted(UiContext context, float x, float y)
    {
        return AcceptInput && (ClientRectangle.Contains(x, y) || selectorMenu.MouseWanted(context, x, y));
    }

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
            pendingSectorSystem = selectedSectorStar.System;
            selectedSectorStar = null;
            selectorMapPosition = null;
            zoomInButton.HeldDown = zoomInButton.Dragging = false;
            viewState.Switch(SectorViewState.System, FadeOutDuration, FadeInDuration);
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
        var mapRect = GetMapRectangle(context);
        addWaypoint?.Invoke(MapToWorldPosition(mapRect, selectorMapPosition.Value));
        selectorMapPosition = null;
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

        if (!viewState.Active(SectorViewState.System))
            return;

        if (draggingMap)
            return;

        if (!mapRect.Contains(context.MouseX, context.MouseY))
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

    private class DrawObject
    {
        public uint Hash;
        public int LabelPriority;
        public string? Name;
        public UiRenderable? Renderable;
        public float SolarRadius;
        public Vector2 XZ;
    }

    private class SectorStar
    {
        public float Phase;
        public Vector2 Position;
        public UiRenderable? Renderable;
        public float Size;
        public StarSystem System = null!;
        public InterfaceColor? Tint;
        public float TintOffset;
    }

    private readonly struct SectorConnection(Vector2 source, Vector2 target)
    {
        public readonly Vector2 Source = source;
        public readonly Vector2 Target = target;
    }

    private class DrawZone
    {
        public readonly float Sort;
        public readonly string Texture;
        public readonly Color4 Tint;
        public readonly Zone Zone;

        public DrawZone(Zone zone, Color4 tint, string texture, float sort)
        {
            Zone = zone;
            Tint = tint;
            Texture = texture;
            Sort = sort;
        }
    }

    private struct Tradelanes
    {
        public Vector2 StartXZ;
        public Vector2 EndXZ;
    }

    private enum SectorViewState
    {
        System,
        Sector
    }

    private struct ZoneVertex : IVertexType
    {
        public VertexDeclaration GetVertexDeclaration()
        {
            return new VertexDeclaration(8,
                new VertexElement(VertexSlots.Position, 2, VertexElementType.Float, false, 0));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct NavmapParameters
    {
        public Vector4 Rectangle;
        public Vector2 Tiling;
    }
}
