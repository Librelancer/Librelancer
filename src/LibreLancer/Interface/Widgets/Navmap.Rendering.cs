// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;

namespace LibreLancer.Interface;

public partial class Navmap
{
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
        bestPathButton.Style = navStyle.BestPathButton;
        addWaypointButton.MouseDownSound = null;
        bestPathButton.MouseDownSound = null;
        userWaypointDiamond = null;
        userWaypointDigits.Clear();
        return navStyle;
    }

    public void PopulateIcons(UiContext ctx, StarSystem sys)
    {
        foreach (var l in ctx.Data.NavmapIcons.Libraries())
            ctx.Data.ResourceManager.LoadResourceFile(ctx.Data.DataPath + l);
        currentDisplaySystem = sys;
        objects = [];
        tradelanes = [];
        navmapscale = sys.NavMapScale;

        foreach (var obj in sys.Objects)
        {
            if (obj.Dock is { Kind: DockKinds.Tradelane } &&
                !string.IsNullOrEmpty(obj.Dock.Target) &&
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

            var nm = ctx.Data.Infocards?.GetStringResource(obj.IdsName);
            if (string.IsNullOrWhiteSpace(nm))
                nm = obj.Nickname;

            objects.Add(new DrawObject
            {
                Renderable = ctx.Data.NavmapIcons.GetSystemObject(archetype.NavmapIcon),
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
                        lH, rVert * Zoom);
                    RenderText(context, drawList, ref letterCache[jj++], numRect, gridIdentSize, gridIdentFont,
                        context.Data.GetColor("text"),
                        new InterfaceColor { Color = Color4.Black }, HorizontalAlignment.Center,
                        VerticalAlignment.Center, false, renNum, systemAlpha);
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
                        VerticalAlignment.Bottom, false, renLet, systemAlpha);
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

        Vector2 WorldToMap(Vector2 a)
        {
            var relPos = (a + scale / 2) / scale;
            return new Vector2(rect.X, rect.Y) + relPos * new Vector2(rect.Width, rect.Height)
                   - new Vector2(OffsetX, OffsetY);
        }

        var zoneclip = context.PointsToPixels(rectNoScale);
        zoneclip.X++;
        zoneclip.Y++;
        zoneclip.Width -= 1;
        zoneclip.Height -= 1;
        if (zoneclip.Width <= 0) zoneclip.Width = 1;
        if (zoneclip.Height <= 0) zoneclip.Height = 1;
        var zoneOrigin =
            context.PointsToPixels(new RectangleF(rect.X - OffsetX, rect.Y - OffsetY, rect.Width, rect.Height));
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
                if (!isVisited(zone.Hash))
                    continue;
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
        if ((CachedRenderString[]?)objectStrings == null || objectStrings.Length < objects.Count)
            objectStrings = new CachedRenderString[objects.Count];
        jj = 0;
        labelCandidates.Clear();

        foreach (var obj in objects)
        {
            if (!isVisited(obj.Hash))
                continue;
            var posAbs = WorldToMap(obj.XZ);
            var szIcon = MathF.Max(2 * obj.SolarRadius / scale.Y * rect.Height, MinimumObjectIconSize);
            var originIcon = szIcon / 2;

            if (obj.Renderable != null)
            {
                var objRect = new RectangleF(posAbs.X - originIcon, posAbs.Y - originIcon, szIcon, szIcon);
                obj.Renderable.Draw(context, drawList, objRect, systemAlpha);
            }

            if (!string.IsNullOrWhiteSpace(obj.Name))
            {
                var textSize = context.RenderContext.Renderer2D.MeasureString(font, context.TextSize(fontSize), obj.Name);
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
                VerticalAlignment.Top, false, label.Object.Name!, systemAlpha);
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
        if (userWaypointProvider != null && currentDisplaySystem != null)
        {
            userWaypoints.Clear();
            userWaypointProvider(currentDisplaySystem, userWaypoints);
        }

        if (userWaypoints.Count == 0 || alpha <= 0)
            return;

        var lineColor = navStyle.UserWaypointColor.GetColor(context.GlobalTime);
        lineColor.A *= alpha;
        var currentPlayerPosition = playerPositionProvider?.Invoke();
        var includePlayerPoint = currentDisplaySystem != null &&
                                 currentPlayerPosition.HasValue &&
                                 playerSystemProvider?.Invoke() == currentDisplaySystem.CRC;
        var routePoints = new Vector2[userWaypoints.Count + (includePlayerPoint ? 1 : 0)];
        var pointIndex = 0;
        if (includePlayerPoint)
            routePoints[pointIndex++] = context.PointsToPixels(worldToMap(
                new Vector2(currentPlayerPosition!.Value.X, currentPlayerPosition.Value.Z)));

        for (var i = 0; i < userWaypoints.Count; i++)
            routePoints[pointIndex++] = context.PointsToPixels(worldToMap(
                new Vector2(userWaypoints[i].Position.X, userWaypoints[i].Position.Z)));

        if (pointIndex > 1)
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

    private static RectangleF Centered(Vector2 center, float width, float height) =>
        new(center.X - width / 2, center.Y - height / 2, width, height);

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
            if (!isVisited(connection.SourceSystemHash) ||
                !isVisited(connection.TargetSystemHash) ||
                !isVisited(connection.SourceObjectHash) ||
                !isVisited(connection.TargetObjectHash))
                continue;
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
            if (!isVisited(star.System.CRC))
                continue;
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
            if (!isVisited(star.System.CRC))
                continue;
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
}
