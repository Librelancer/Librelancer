// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent
{
    public class UniverseMap
    {
        public static readonly VertexDiffuse Background = (VertexDiffuse)(new Color4(0.03f, 0.04f, 0.05f, 1f));
        public static readonly VertexDiffuse LegalConnection = (VertexDiffuse)(new Color4(0.25f, 0.50f, 0.80f, 1f));
        public static readonly VertexDiffuse IllegalConnection = (VertexDiffuse)(new Color4(0.42f, 0.45f, 0.50f, 1f));
        public static readonly VertexDiffuse Label = (VertexDiffuse)(new Color4(0.86f, 0.90f, 0.95f, 0.74f));
        public static readonly VertexDiffuse LabelHighlighted = (VertexDiffuse)(new Color4(0.86f, 0.90f, 0.95f, 1f));
        public static readonly VertexDiffuse EditGrid = (VertexDiffuse)Color4.Gray;
        public static readonly VertexDiffuse EditableNodeHovered = (VertexDiffuse)(new Color4(0.85f, 0.88f, 0.92f, 1f));
        public static readonly VertexDiffuse EditableNode = (VertexDiffuse)(new Color4(0.72f, 0.75f, 0.80f, 1f));
        public static readonly VertexDiffuse HighlightedNode = (VertexDiffuse)(new Color4(1f, 0.78f, 0.25f, 1f));
        public static readonly VertexDiffuse Node = (VertexDiffuse)(new Color4(0.82f, 0.86f, 0.91f, 0.95f));

        private EditorSystem dragTarget = null;
        private Vector2 dragOgPos = Vector2.Zero;
        private Vector2 viewPan = Vector2.Zero; //for when a 2d map needs zooming
        private float viewZoom = 1f;

        public EditorUndoBuffer UndoBuffer = new EditorUndoBuffer();

        public event Action OnChange;

        class ChangePositionAction(EditorSystem target, Vector2 old, Vector2 updated)
            : EditorModification<Vector2>(old, updated)
        {
            public readonly EditorSystem Target = target;

            public override void Set(Vector2 value) =>
                Target.Position = value;
        }

        private Vector2 newPos = Vector2.Zero;

        public sealed class Connection(EditorSystem source, EditorSystem target, bool legal = true)
        {
            public EditorSystem Source = source;
            public EditorSystem Target = target;
            public bool Legal = legal;
        }

        public sealed class Route(IReadOnlyList<EditorSystem> path, uint color, float thickness)
        {
            public IReadOnlyList<EditorSystem> Path = path;
            public uint Color = color;
            public float Thickness = thickness;
        }

        public sealed class ViewOptions
        {
            public string Id = "##universemapview";
            public ImTextureRef? Background;
            public Vector2 BackgroundUvMin = new(0, 1);
            public Vector2 BackgroundUvMax = new(1, 0);
            public bool EnablePanZoom = true;
            public bool ShowHelpText = true;
            public bool ShowLabels = true;
            public bool FitToSystems = true;
            public bool EditableSystems = false;
            public float Margin = 0.15f;
            public float NodeSpacing = 0;
            public float RelatedNodeSpacing = 0;
            public float ConnectionThickness = 1.5f;
            public float EditableNodeSize = 5f;
            public string HelpText = "Mouse wheel zooms. Right-drag pans.";
            public IReadOnlyCollection<string> HighlightedSystems;
            public Func<EditorSystem, bool> IsVisible;
            public Func<EditorSystem, string> Label;
            public Func<EditorSystem, string> Tooltip;
            public Func<EditorSystem, EditorSystem, bool> AreRelated;
            public Action<EditorSystem> OnClick;
            public Action<EditorSystem> OnDoubleClick;
            public string EmptyText = "No systems visible.";
        }

        public void ResetView()
        {
            viewPan = Vector2.Zero;
            viewZoom = 1f;
        }

        public void Draw(
            List<EditorSystem> systems,
            GameDataManager gameData,
            IReadOnlyList<Connection> connections,
            IReadOnlyList<Route> routes,
            Vector2 size,
            ViewOptions options)
        {
            if (size.X < 40 || size.Y < 40 || systems.Count == 0)
                return;

            var topLeft = ImGui.GetCursorScreenPos();
            ImGui.InvisibleButton(options.Id, size);
            var hovered = ImGui.IsItemHovered();
            var drawList = ImGui.GetWindowDrawList();
            var bottomRight = topLeft + size;

            if (options.Background != null)
                drawList.AddImage(options.Background.Value, topLeft, bottomRight, options.BackgroundUvMin, options.BackgroundUvMax);
            else
                drawList.AddRectFilled(topLeft, bottomRight, Background);
            drawList.AddRect(topLeft, bottomRight, ImGui.GetColorU32(ImGuiCol.Border));

            if (hovered && options.EnablePanZoom)
            {
                var io = ImGui.GetIO();
                if (io.MouseWheel != 0)
                    viewZoom = Math.Clamp(viewZoom * (1f + io.MouseWheel * 0.16f), 0.48f, 40f);

                if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
                    viewPan += io.MouseDelta;
            }

            var visibleSystems = systems
                .Where(system => options.IsVisible?.Invoke(system) ?? true)
                .ToList();
            if (visibleSystems.Count == 0)
            {
                drawList.AddText(topLeft + new Vector2(10 * ImGuiHelper.Scale),
                    ImGui.GetColorU32(ImGuiCol.TextDisabled), options.EmptyText);
                return;
            }

            var screenPositions = CreateScreenPositions(visibleSystems, topLeft, size, options);
            SeparateNodes(screenPositions, visibleSystems, options.NodeSpacing, options.RelatedNodeSpacing, options.AreRelated);

            Vector2 ToScreen(EditorSystem system) => screenPositions[system];

            drawList.PushClipRect(topLeft, bottomRight, true);
            if (options.EditableSystems && dragTarget != null && !options.FitToSystems)
                DrawEditGrid(drawList, topLeft, size, options.Margin);

            foreach (var connection in connections)
            {
                if (!screenPositions.ContainsKey(connection.Source) || !screenPositions.ContainsKey(connection.Target))
                    continue;

                var col = connection.Legal
                    ? LegalConnection
                    : IllegalConnection;
                drawList.AddLine(ToScreen(connection.Source), ToScreen(connection.Target), col,
                    options.ConnectionThickness * ImGuiHelper.Scale);
            }

            foreach (var route in routes)
                DrawRoutePath(drawList, route, ToScreen);

            var highlighted = options.HighlightedSystems;
            var mouse = ImGui.GetIO().MousePos;
            EditorSystem hoveredSystem = null;
            EditorSystem dragCurrent = null;
            var grabbed = false;
            foreach (var system in visibleSystems.OrderBy(x => x.System.Nickname))
            {
                var point = ToScreen(system);
                if (point.X < topLeft.X - 20 || point.Y < topLeft.Y - 20 ||
                    point.X > bottomRight.X + 20 || point.Y > bottomRight.Y + 20)
                    continue;

                var hitRadius = 10 * ImGuiHelper.Scale;
                var isHoveredSystem = hovered && Vector2.Distance(mouse, point) < hitRadius;
                DrawSystemNode(drawList, point, system, isHoveredSystem, highlighted, options);

                if (options.ShowLabels)
                {
                    var label = options.Label?.Invoke(system) ?? DefaultSystemLabel(gameData, system);
                    var labelColor = highlighted?.Contains(system.System.Nickname) == true
                        ? LabelHighlighted
                        : Label;
                    drawList.AddText(point + new Vector2(6 * ImGuiHelper.Scale, -11 * ImGuiHelper.Scale),
                        labelColor, label);
                }

                if (isHoveredSystem)
                    hoveredSystem = system;

                if (isHoveredSystem && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    options.OnDoubleClick?.Invoke(system);
                else if (isHoveredSystem && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    options.OnClick?.Invoke(system);

                if (options.EditableSystems && (isHoveredSystem || dragTarget == system) &&
                    ImGui.IsMouseDragging(ImGuiMouseButton.Left) &&
                    !grabbed && (dragTarget == null || dragTarget == system) && !options.FitToSystems)
                {
                    grabbed = true;
                    if (dragTarget == null)
                    {
                        dragTarget = system;
                        dragOgPos = system.Position;
                        newPos = system.Position;
                    }

                    var factor = (size * (1 - 2 * options.Margin)) / 16f;
                    newPos += ImGui.GetIO().MouseDelta / factor;
                    system.Position = MathHelper.Snap(newPos,
                        ImGui.IsKeyDown(ImGuiKey.ImGuiMod_Shift) ? Vector2.Zero : Vector2.One);
                    dragCurrent = system;
                }
            }
            drawList.PopClipRect();

            if (options.EditableSystems && dragCurrent == null && dragTarget != null)
            {
                UndoBuffer.Commit(new ChangePositionAction(dragTarget, dragOgPos, dragTarget.Position));
                OnChange?.Invoke();
                dragTarget = null;
            }

            if (hoveredSystem != null)
            {
                var tooltip = options.Tooltip?.Invoke(hoveredSystem) ?? DefaultSystemLabel(gameData, hoveredSystem);
                ImGui.SetTooltip(tooltip);
            }
            else if (hovered && options.EnablePanZoom && options.ShowHelpText)
            {
                ImGui.SetTooltip(options.HelpText);
            }
        }

        private static void DrawEditGrid(ImDrawListPtr drawList, Vector2 topLeft, Vector2 size, float margin)
        {
            var connectMin = topLeft + size * margin;
            var connectMax = topLeft + size - size * margin;
            var gridSize = size - (size * margin * 2);
            var factor = gridSize / 16f;

            drawList.AddRect(connectMin, connectMax, EditGrid);
            for (int x = 1; x < 16; x++)
            {
                drawList.AddLine(connectMin + new Vector2(x, 0) * factor,
                    connectMin + new Vector2(x, 0) * factor + new Vector2(0, gridSize.Y), EditGrid);
            }
            for (int y = 1; y < 16; y++)
            {
                drawList.AddLine(connectMin + new Vector2(0, y) * factor,
                    connectMin + new Vector2(0, y) * factor + new Vector2(gridSize.X, 0), EditGrid);
            }
        }

        private static void DrawSystemNode(
            ImDrawListPtr drawList,
            Vector2 point,
            EditorSystem system,
            bool hovered,
            IReadOnlyCollection<string> highlighted,
            ViewOptions options)
        {
            if (options.EditableSystems)
            {
                var size = MathF.Max(2, options.EditableNodeSize * ImGuiHelper.Scale);
                var min = point - new Vector2(size * 0.5f);
                var max = point + new Vector2(size * 0.5f);
                var color = hovered
                    ? EditableNodeHovered
                    : EditableNode;
                drawList.AddRectFilled(min, max, color);
                return;
            }

            var isHighlighted = highlighted?.Contains(system.System.Nickname) == true;
            var radius = (isHighlighted ? 5.5f : 4.0f) * ImGuiHelper.Scale;
            var nodeColor = isHighlighted
                ? HighlightedNode
                : Node;
            drawList.AddCircleFilled(point, radius, nodeColor, 16);
        }

        private Dictionary<EditorSystem, Vector2> CreateScreenPositions(
            List<EditorSystem> systems,
            Vector2 topLeft,
            Vector2 size,
            ViewOptions options)
        {
            if (!options.FitToSystems)
            {
                var factor = (size * (1 - 2 * options.Margin)) / 16f;
                var start = topLeft + size * options.Margin;
                return systems.ToDictionary(system => system, system => start + system.Position * factor);
            }

            var boundsMin = new Vector2(systems.Min(x => x.Position.X), systems.Min(x => x.Position.Y));
            var boundsMax = new Vector2(systems.Max(x => x.Position.X), systems.Max(x => x.Position.Y));
            var span = boundsMax - boundsMin;
            if (span.X < 0.001f) span.X = 1;
            if (span.Y < 0.001f) span.Y = 1;

            var padding = 32 * ImGuiHelper.Scale;
            var scale = MathF.Min((size.X - padding * 2) / span.X, (size.Y - padding * 2) / span.Y) * viewZoom;
            var center = (boundsMin + boundsMax) * 0.5f;

            return systems.ToDictionary(
                system => system,
                system => topLeft + size * 0.5f + viewPan + ((system.Position - center) * scale));
        }

        private static void SeparateNodes(
            Dictionary<EditorSystem, Vector2> screenPositions,
            List<EditorSystem> systems,
            float nodeSpacing,
            float relatedNodeSpacing,
            Func<EditorSystem, EditorSystem, bool> areRelated)
        {
            if (systems.Count < 2 || (nodeSpacing <= 0 && relatedNodeSpacing <= 0))
                return;

            for (var iteration = 0; iteration < 32; iteration++)
            {
                var moved = false;
                for (var i = 0; i < systems.Count; i++)
                {
                    for (var j = i + 1; j < systems.Count; j++)
                    {
                        var first = systems[i];
                        var second = systems[j];
                        var wanted = areRelated?.Invoke(first, second) == true
                            ? relatedNodeSpacing
                            : nodeSpacing;
                        if (wanted <= 0)
                            continue;

                        var a = screenPositions[first];
                        var b = screenPositions[second];
                        var delta = b - a;
                        var distance = delta.Length();
                        if (distance >= wanted)
                            continue;

                        if (distance < 0.001f)
                        {
                            delta = new Vector2(1, 0);
                            distance = 1;
                        }

                        var push = (wanted - distance) * 0.5f;
                        var normal = delta / distance;
                        screenPositions[first] = a - normal * push;
                        screenPositions[second] = b + normal * push;
                        moved = true;
                    }
                }

                if (!moved)
                    break;
            }
        }

        private static void DrawRoutePath(ImDrawListPtr draw, Route route, Func<EditorSystem, Vector2> toScreen)
        {
            for (var i = 0; i < route.Path.Count - 1; i++)
                draw.AddLine(toScreen(route.Path[i]), toScreen(route.Path[i + 1]), route.Color, route.Thickness);

            if (route.Path.Count == 1)
            {
                var p = toScreen(route.Path[0]);
                draw.AddCircle(p, 8, route.Color, 24, route.Thickness);
            }
        }

        private static string DefaultSystemLabel(GameDataManager gameData, EditorSystem system)
        {
            var name = gameData.GetString(system.System.IdsName);
            return string.IsNullOrWhiteSpace(name) ? system.System.Nickname : $"{system.System.Nickname} ({name})";
        }
    }
}
