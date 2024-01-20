using System;
using System.Numerics;
using ImGuiNET;

namespace LibreLancer.ImUI;

public enum VectorIcon
{
    Flow,
    Circle,
    Square,
    Grid,
    RoundSquare,
    Diamond
}

public static class VectorIcons
{
    public static void Icon(Vector2 size, VectorIcon type, bool filled, Color4? color = null, Color4? innerColor = null)
    {
        if (ImGui.IsRectVisible(size))
        {
            var cursorPos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();
            DrawIcon(drawList, cursorPos, cursorPos + size, type, filled, color ?? Color4.White,
                color ?? Color4.TransparentBlack);
        }

        ImGui.Dummy(size);
    }

    public static void DrawIcon(ImDrawListPtr drawList, Vector2 rectMin, Vector2 rectMax, VectorIcon type, bool filled,
        Color4 color, Color4 innerColor)
    {
        var innerColor_u32 = ImGui.GetColorU32(innerColor);
        var color_u32 = ImGui.GetColorU32(color);

        var rect_x = rectMin.X;
        var rect_y = rectMin.Y;
        var rect_w = rectMax.X - rectMin.X;
        var rect_h = rectMax.Y - rectMin.Y;

        var rect_center_x = (rectMin.X + rectMax.X) * 0.5f;
        var rect_center_y = (rectMin.Y + rectMax.Y) * 0.5f;
        var rect_center = new Vector2(rect_center_x, rect_center_y);
        var outline_scale = rect_w / 24.0f;
        var extra_segments = (int)(2 * outline_scale); // for full circle

        if (type == VectorIcon.Flow)
        {
            var origin_scale = rect_w / 24.0f;

            var offset_x = 1.0f * origin_scale;
            var offset_y = 0.0f * origin_scale;
            var margin = (filled ? 2.0f : 2.0f) * origin_scale;
            var rounding = 0.1f * origin_scale;
            var tip_round = 0.7f; // percentage of triangle edge (for tip)
            //var edge_round = 0.7f; // percentage of triangle edge (for corner)
            var canvasMin = new Vector2(
                rectMin.X + margin + offset_x,
                rectMin.Y + margin + offset_y);
            var canvasMax = new Vector2(
                rectMax.X - margin + offset_x,
                rectMax.Y - margin + offset_y);
            var canvas_x = canvasMin.X;
            var canvas_y = canvasMin.Y;
            var canvas_w = canvasMax.X - canvasMin.X;
            var canvas_h = canvasMax.Y - canvasMin.Y;

            var left = canvas_x + canvas_w * 0.5f * 0.3f;
            var right = canvas_x + canvas_w - canvas_w * 0.5f * 0.3f;
            var top = canvas_y + canvas_h * 0.5f * 0.2f;
            var bottom = canvas_y + canvas_h - canvas_h * 0.5f * 0.2f;
            var center_y = (top + bottom) * 0.5f;
            //var angle = AX_PI * 0.5f * 0.5f * 0.5f;

            var tip_top = new Vector2(canvas_x + canvas_w * 0.5f, top);
            var tip_right = new Vector2(right, center_y);
            var tip_bottom = new Vector2(canvas_x + canvas_w * 0.5f, bottom);

            drawList.PathLineTo(new Vector2(left, top) + new Vector2(0, rounding));
            drawList.PathBezierCubicCurveTo(
                new Vector2(left, top),
                new Vector2(left, top),
                new Vector2(left, top) + new Vector2(rounding, 0));
            drawList.PathLineTo(tip_top);
            drawList.PathLineTo(tip_top + (tip_right - tip_top) * tip_round);
            drawList.PathBezierCubicCurveTo(
                tip_right,
                tip_right,
                tip_bottom + (tip_right - tip_bottom) * tip_round);
            drawList.PathLineTo(tip_bottom);
            drawList.PathLineTo(new Vector2(left, bottom) + new Vector2(rounding, 0));
            drawList.PathBezierCubicCurveTo(
                new Vector2(left, bottom),
                new Vector2(left, bottom),
                new Vector2(left, bottom) - new Vector2(0, rounding));

            if (!filled)
            {
                if ((innerColor_u32 & 0xFF000000) != 0)
                    drawList.AddConvexPolyFilled(ref drawList._Path[0], drawList._Path.Size, innerColor_u32);

                drawList.PathStroke(color_u32, ImDrawFlags.Closed, 2.0f * outline_scale);
            }
            else
                drawList.PathFillConvex(color_u32);
        }
        else
        {
            var triangleStart = rect_center_x + 0.32f * rect_w;

            var rect_offset = -(int)(rect_w * 0.25f * 0.25f);

            rectMin.X += rect_offset;
            rectMax.X += rect_offset;
            rect_x += rect_offset;
            rect_center_x += rect_offset * 0.5f;
            rect_center.X += rect_offset * 0.5f;

            if (type == VectorIcon.Circle)
            {
                var c = rect_center;

                if (!filled)
                {
                    var r = 0.5f * rect_w / 2.0f - 0.5f;

                    if ((innerColor_u32 & 0xFF000000) != 0)
                        drawList.AddCircleFilled(c, r, innerColor_u32, 12 + extra_segments);
                    drawList.AddCircle(c, r, color_u32, 12 + extra_segments, 2.0f * outline_scale);
                }
                else
                {
                    drawList.AddCircleFilled(c, 0.5f * rect_w / 2.0f, color_u32, 12 + extra_segments);
                }
            }

            if (type == VectorIcon.Square)
            {
                if (filled)
                {
                    var r = 0.5f * rect_w / 2.0f;
                    var p0 = rect_center - new Vector2(r, r);
                    var p1 = rect_center + new Vector2(r, r);

                    drawList.AddRectFilled(p0, p1, color_u32, 0, ImDrawFlags.RoundCornersAll);
                }
                else
                {
                    var r = 0.5f * rect_w / 2.0f - 0.5f;
                    var p0 = rect_center - new Vector2(r, r);
                    var p1 = rect_center + new Vector2(r, r);

                    if ((innerColor_u32 & 0xFF000000) != 0)
                    {
                        drawList.AddRectFilled(p0, p1, innerColor_u32, 0, ImDrawFlags.RoundCornersAll);
                    }

                    drawList.AddRect(p0, p1, color_u32, 0, ImDrawFlags.RoundCornersAll, 2.0f * outline_scale);
                }
            }

            if (type == VectorIcon.Grid)
            {
                var r = 0.5f * rect_w / 2.0f;
                var w = MathF.Ceiling(r / 3.0f);

                var baseTl = new Vector2(MathF.Floor(rect_center_x - w * 2.5f), MathF.Floor(rect_center_y - w * 2.5f));
                var baseBr = new Vector2(MathF.Floor(baseTl.X + w), MathF.Floor(baseTl.Y + w));

                var tl = baseTl;
                var br = baseBr;
                for (int i = 0; i < 3; ++i)
                {
                    tl.X = baseTl.X;
                    br.X = baseBr.X;
                    drawList.AddRectFilled(tl, br, color_u32);
                    tl.X += w * 2;
                    br.X += w * 2;
                    if (i != 1 || filled)
                        drawList.AddRectFilled(tl, br, color_u32);
                    tl.X += w * 2;
                    br.X += w * 2;
                    drawList.AddRectFilled(tl, br, color_u32);

                    tl.Y += w * 2;
                    br.Y += w * 2;
                }

                triangleStart = br.X + w + 1.0f / 24.0f * rect_w;
            }

            if (type == VectorIcon.RoundSquare)
            {
                if (filled)
                {
                    var r = 0.5f * rect_w / 2.0f;
                    var cr = r * 0.5f;
                    var p0 = rect_center - new Vector2(r, r);
                    var p1 = rect_center + new Vector2(r, r);

                    drawList.AddRectFilled(p0, p1, color_u32, cr, ImDrawFlags.RoundCornersAll);
                }
                else
                {
                    var r = 0.5f * rect_w / 2.0f - 0.5f;
                    var cr = r * 0.5f;
                    var p0 = rect_center - new Vector2(r, r);
                    var p1 = rect_center + new Vector2(r, r);

                    if ((innerColor_u32 & 0xFF000000) != 0)
                    {
                        drawList.AddRectFilled(p0, p1, innerColor_u32, cr, ImDrawFlags.RoundCornersAll);
                    }

                    drawList.AddRect(p0, p1, color_u32, cr, ImDrawFlags.RoundCornersAll, 2.0f * outline_scale);
                }
            }
            else if (type == VectorIcon.Diamond)
            {
                if (filled)
                {
                    var r = 0.607f * rect_w / 2.0f;
                    var c = rect_center;

                    drawList.PathLineTo(c + new Vector2(0, -r));
                    drawList.PathLineTo(c + new Vector2(r, 0));
                    drawList.PathLineTo(c + new Vector2(0, r));
                    drawList.PathLineTo(c + new Vector2(-r, 0));
                    drawList.PathFillConvex(color_u32);
                }
                else
                {
                    var r = 0.607f * rect_w / 2.0f - 0.5f;
                    var c = rect_center;

                    drawList.PathLineTo(c + new Vector2(0, -r));
                    drawList.PathLineTo(c + new Vector2(r, 0));
                    drawList.PathLineTo(c + new Vector2(0, r));
                    drawList.PathLineTo(c + new Vector2(-r, 0));

                    if ((innerColor_u32 & 0xFF000000) != 0)
                        drawList.AddConvexPolyFilled(ref drawList._Path[0], drawList._Path.Size, innerColor_u32);

                    drawList.PathStroke(color_u32, ImDrawFlags.Closed, 2.0f * outline_scale);
                }
            }
            else
            {
                var triangleTip = triangleStart + rect_w * (0.45f - 0.32f);

                drawList.AddTriangleFilled(
                    new Vector2(MathF.Ceiling(triangleTip), rect_y + rect_h * 0.5f),
                    new Vector2(triangleStart, rect_center_y + 0.15f * rect_h),
                    new Vector2(triangleStart, rect_center_y - 0.15f * rect_h),
                    color_u32);
            }
        }
    }
}
