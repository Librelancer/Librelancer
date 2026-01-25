using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.GameData.World;
using LibreLancer.ImUI;
using LibreLancer.Data.Schema.Universe;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LancerEdit.GameContent;

public class Map2DCreationTools
{
    public PatrolEditor Patrol { get; } = new();
    public ZoneShapeCreator ZoneShape { get; } = new();

    public TradelaneEditor Tradelane { get; } = new();

    public string Draw(ImDrawListPtr dlist, Vector2 wPos, float renderWidth, Func<Vector3, Vector2> worldToMap, Func<Vector2, Vector3> mapToWorld, SystemEditorTab tab)
    {
        // Draw patrol path and handle patrol interactions
        var helpText = Patrol.Draw(dlist, wPos, worldToMap, mapToWorld, tab);
        if (helpText != null)
            return helpText;
        // Zone shape creation logic
        helpText = ZoneShape.Draw(dlist, wPos, renderWidth, mapToWorld, tab);
        if (helpText != null)
            return helpText;
        return Tradelane.Draw(dlist, wPos, worldToMap, mapToWorld, tab);
    }

    public bool IsAnyToolActive => Patrol.IsActive || ZoneShape.IsActive || Tradelane.IsActive;
}

public class TradelaneEditor
{
    public bool IsActive { get; private set; }

    private Vector3? start;
    private Vector3? end;

    public void Start()
    {
        IsActive = true;
        start = end = null;
    }

    public void Cancel()
    {
        IsActive = false;
        start = end = null;
    }

    void AddPoint(Vector3 point, SystemEditorTab tab)
    {
        if (IsActive)
        {
            if (start == null)
            {
                start = point;
            }
            else
            {
                end = point;
                tab.OnCreateTradelane(start!.Value, point);
            }
        }
    }


    private const float TRADELANE_DISTANCE = 8300;

    public string Draw(ImDrawListPtr dlist, Vector2 wPos, Func<Vector3, Vector2> worldToMap,
        Func<Vector2, Vector3> mapToWorld, SystemEditorTab tab)
    {
        if (!IsActive) return null;

        var mousePos = ImGui.GetMousePos();
        var io = ImGui.GetIO();


        var mouseInWindow = mousePos - wPos;
        var mouseWorldPos = mapToWorld(mouseInWindow);

        // Draw line from last point to mouse (with Y offset applied)
        if (end != null)
        {
            var startPos = wPos + worldToMap(start!.Value);
            var endPos = wPos + worldToMap(end.Value);
            dlist.AddLine(startPos, endPos, 0xFF80FF80, 1f * ImGuiHelper.Scale);
        }
        else if (start != null)
        {
            var lastPoint = wPos + worldToMap(start.Value);
            var offset = mouseWorldPos - start.Value;
            if (!io.KeyShift && offset.Length() >= TRADELANE_DISTANCE)
            {
                var dir = offset.Normalized();
                var len = MathF.Floor(offset.Length() / TRADELANE_DISTANCE) * TRADELANE_DISTANCE;
                mouseWorldPos = start.Value + (dir * len);
            }
            //
            var mouseMapPos = wPos + worldToMap(mouseWorldPos);
            dlist.AddLine(lastPoint, mouseMapPos, 0xFF80FF80, 1f * ImGuiHelper.Scale);
        }

        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            AddPoint(mouseWorldPos, tab);
        }

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            Cancel();
        }

        return "Tradelane: Left click to set start and end points. Right click to cancel\nHold shift to disable snapping.";
    }
}

// Patrol state management
public class PatrolEditor
{
    public bool IsActive { get; private set; }
    public List<Vector3> Points { get; } = new();
    public float YOffset { get; private set; } = 0f;

    public void Start()
    {
        IsActive = true;
        Points.Clear();
        YOffset = 0f;
    }

    public void Cancel()
    {
        IsActive = false;
        Points.Clear();
        YOffset = 0f;
    }

    void AddPoint(Vector3 point)
    {
        if (IsActive)
        {
            var adjustedPoint = new Vector3(point.X, YOffset, point.Z);
            Points.Add(adjustedPoint);
        }
    }

    public List<Vector3> Finish()
    {
        var result = new List<Vector3>(Points);
        IsActive = false;
        Points.Clear();
        YOffset = 0f;
        return result;
    }

    public string Draw(ImDrawListPtr dlist, Vector2 wPos, Func<Vector3, Vector2> worldToMap, Func<Vector2, Vector3> mapToWorld, SystemEditorTab tab)
    {
        if (!IsActive) return null;

        var mousePos = ImGui.GetMousePos();
        var io = ImGui.GetIO();

        // Handle scroll wheel for Y-axis adjustment
        if (ImGui.IsItemHovered() && io.MouseWheel != 0)
        {
            YOffset += io.MouseWheel * 100f;
            YOffset = MathHelper.Clamp(YOffset, -1000000f, 1000000f);
        }

        // Draw existing patrol path
        for (int i = 0; i < Points.Count; i++)
        {
            var pointPos = worldToMap(Points[i]);
            var screenPoint = wPos + pointPos; // Add window position

            dlist.AddCircleFilled(screenPoint, 4f * ImGuiHelper.Scale, 0xFF00FF00);

            if (i < Points.Count - 1)
            {
                var nextPointPos = worldToMap(Points[i + 1]);
                dlist.AddLine(screenPoint, wPos + nextPointPos, 0xFF00FF00, 2f * ImGuiHelper.Scale);
            }
        }

        // Draw line from last point to mouse (with Y offset applied)
        if (Points.Count > 0)
        {
            var lastPoint = wPos + worldToMap(Points[Points.Count - 1]);
            var mouseInWindow = mousePos - wPos;
            var currentMouseWorldPos = mapToWorld(mouseInWindow);
            var mouseWithOffset = new Vector3(currentMouseWorldPos.X, YOffset, currentMouseWorldPos.Z);
            var mouseMapPos = wPos + worldToMap(mouseWithOffset);
            dlist.AddLine(lastPoint, mouseMapPos, 0xFF80FF80, 1f * ImGuiHelper.Scale);
        }

        // Handle mouse interactions
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var mouseInWindow = mousePos - wPos;
            var worldPos = mapToWorld(mouseInWindow);
            // Apply Y offset to the new point
            worldPos.Y = YOffset;
            AddPoint(worldPos);
        }

        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            tab.FinishPatrolRoute();
        }

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            Cancel();
        }

        return $"Patrol: Left click to create point, double click to finish, right click to cancel\nScroll wheel to adjust Y-axis (height): {YOffset:F0}";
    }
}

public class ZoneShapeCreator
{
    private float ellipsoidAxisA = 0f;
    private float ellipsoidAxisB = 0f;
    private bool ellipsoidSecondPointSet = false;
    private Vector2 centerScreen;
    private Vector3 centerWorld;
    private float radius;
    private ShapeKind shapeKind;
    public bool IsActive { get; private set; } = false;
    private SystemEditorTab tab;
    private float ellipsoidAngle = 0f;

    public void Start(ShapeKind shape, Vector2 screenPos, Vector3 worldPos)
    {
        IsActive = true;
        shapeKind = shape;
        centerScreen = screenPos;
        centerWorld = worldPos;
        radius = 0f;
        ellipsoidAxisA = 0f;
        ellipsoidAxisB = 0f;
        ellipsoidSecondPointSet = false;
    }

    public string Draw(ImDrawListPtr dlist, Vector2 wPos, float renderWidth, System.Func<Vector2, Vector3> mapToWorld, SystemEditorTab tab)
    {
        if (!IsActive) return null;
        var mouseScreen = ImGui.GetMousePos() - wPos;
        switch (shapeKind)
        {
            case ShapeKind.Sphere:
                radius = Vector2.Distance(centerScreen, mouseScreen);
                dlist.AddCircle(wPos + centerScreen, radius, ImGui.GetColorU32(Color4.LightSkyBlue), 64);

                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) && !ImGui.IsPopupOpen(null, ImGuiPopupFlags.AnyPopup))
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        var radiusPoint = centerScreen + new Vector2(radius, 0);
                        var radiusWorld = Vector3.Distance(centerWorld, mapToWorld(radiusPoint)) * 1.08f;

                        var zone = new Zone()
                        {
                            Nickname = $"zone_{Guid.NewGuid().ToString().Substring(0, 8)}",
                            Position = centerWorld,
                            Size = new Vector3(radiusWorld, radiusWorld, radiusWorld),
                            Shape = ShapeKind.Sphere,
                            RotationMatrix = Matrix4x4.Identity,
                            DensityRestrictions = Array.Empty<DensityRestriction>(),
                            Encounters = Array.Empty<Encounter>()
                        };
                        tab.UndoBuffer.Commit(new SysAddZoneAction(tab, zone));
                        IsActive = false;
                    }
                    else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                    {
                        IsActive = false;
                    }
                }
                return $"Sphere: Radius={radius:F0}\nLeft click to create, right click to cancel";
            case ShapeKind.Ellipsoid:
                ellipsoidAxisA = Vector2.Distance(centerScreen, mouseScreen);

                var io = ImGui.GetIO();
                if (io.MouseWheel != 0)
                {
                    ellipsoidAxisB += io.MouseWheel * 8f;
                    if (ellipsoidAxisB < 1f) ellipsoidAxisB = 1f;
                }
                if (ellipsoidAxisB < 1f) ellipsoidAxisB = ellipsoidAxisA;

                ellipsoidAngle = MathF.Atan2(mouseScreen.Y - centerScreen.Y, mouseScreen.X - centerScreen.X);

                DrawRotatedEllipse(dlist, wPos + centerScreen, ellipsoidAxisA, ellipsoidAxisB, ellipsoidAngle, ImGui.GetColorU32(Color4.LightSkyBlue), 32);

                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) && !ImGui.IsPopupOpen(null, ImGuiPopupFlags.AnyPopup))
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        var dir = Vector2.Normalize(mouseScreen - centerScreen);
                        var perp = new Vector2(-dir.Y, dir.X);
                        var axisAPoint = centerScreen + dir * ellipsoidAxisA;
                        var axisBPoint = centerScreen + perp * ellipsoidAxisB;

                        var axisAWorld = Vector3.Distance(centerWorld, mapToWorld(axisAPoint)) * 1.08f;
                        var axisBWorld = Vector3.Distance(centerWorld, mapToWorld(axisBPoint)) * 1.08f;

                        var rot = Matrix4x4.CreateRotationY(-MathF.Atan2(dir.Y, dir.X));

                        var zone = new Zone()
                        {
                            Nickname = $"zone_{Guid.NewGuid().ToString().Substring(0, 8)}",
                            Position = centerWorld,
                            Size = new Vector3(axisAWorld, axisBWorld, axisBWorld),
                            Shape = ShapeKind.Ellipsoid,
                            RotationMatrix = rot,
                            DensityRestrictions = Array.Empty<DensityRestriction>(),
                            Encounters = Array.Empty<Encounter>()
                        };
                        tab.UndoBuffer.Commit(new SysAddZoneAction(tab, zone));
                        IsActive = false;
                    }
                    else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                    {
                        IsActive = false;
                    }
                }
                return $"Ellipsoid: Mouse = major axis, wheel = minor axis (A={ellipsoidAxisA:F0}, B={ellipsoidAxisB:F0})\nAngle={ellipsoidAngle * 180 / MathF.PI:F1}°, left click to create, right click to cancel";
            default:
                return null;
        }
    }

    private void DrawRotatedEllipse(ImDrawListPtr dlist, Vector2 center, float a, float b, float angle, uint color, int numSegments)
    {
        var points = new Vector2[numSegments];
        for (int i = 0; i < numSegments; i++)
        {
            float theta = (float)(2 * Math.PI * i / numSegments);
            float x = a * MathF.Cos(theta);
            float y = b * MathF.Sin(theta);

            float xr = x * MathF.Cos(angle) - y * MathF.Sin(angle);
            float yr = x * MathF.Sin(angle) + y * MathF.Cos(angle);
            points[i] = center + new Vector2(xr, yr);
        }
        for (int i = 0; i < numSegments; i++)
        {
            dlist.AddLine(points[i], points[(i + 1) % numSegments], color, 2);
        }
    }
}
