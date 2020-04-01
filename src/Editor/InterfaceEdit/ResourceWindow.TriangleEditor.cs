// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using ImGuiNET;
using LibreLancer;
using System.Numerics;
using LibreLancer.ImUI;
using LibreLancer.Interface;

namespace InterfaceEdit
{
    public partial class ResourceWindow
    {
        void EditTriangle(InterfaceImage mdl)
        {
            ImGui.Text("Texture Coordinates");
            EditPoints("##texcoords",  mdl.TexCoords, (x,y,w,h) =>
            {
                ImGui.Image((IntPtr) foundTextureId, new Vector2(w, h), new Vector2(0, 1), new Vector2(1, 0));
            },new Point(foundTexture.Width, foundTexture.Height));
            ImGui.Text("Vertices");
            EditPoints("##vertices", mdl.DisplayCoords, (x, y, w, h) =>
            {
                var drawList = ImGui.GetWindowDrawList();
                var mf = new Vector2(x, y);
                var dim = new Vector2(w,h);
                var dc = mdl.DisplayCoords;
                var tc = mdl.TexCoords;
                var pA = mf + new Vector2(dc.X0, dc.Y0) * dim;
                var pB = mf + new Vector2(dc.X1, dc.Y1) * dim;
                var pC = mf + new Vector2(dc.X2, dc.Y2) * dim;
                var tA = new Vector2(tc.X0, 1- tc.Y0);
                var tB = new Vector2(tc.X1, 1- tc.Y1);
                var tC = new Vector2(tc.X2, 1- tc.Y2);
                drawList.AddImageQuad((IntPtr)foundTextureId,pA,pB,pC,pC,tA,tB,tC,tC, UInt32.MaxValue);
            });
        }
        void EditPoints(string label, InterfacePoints points, Action<int,int,int,int> renderResult, Point? pixels = null)
        {
            ImGui.PushID(label);
            var szX = (int) (ImGui.GetColumnWidth() * 0.6f);
            if (szX > 500) szX = 500;
            var ratio = foundTexture.Height / (float)foundTexture.Width;
            var szY = (int) (szX * ratio);
            //Positioning
            var cPos = (Vector2)ImGui.GetCursorPos();
            var wPos = (Vector2)ImGui.GetWindowPos();
            var scrPos = -ImGui.GetScrollY();
            var mOffset = cPos + wPos + new Vector2(0, scrPos);
            var sz = new Vector2(szX, szY);
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRect(mOffset, mOffset + sz, UInt32.MaxValue);
            renderResult((int) mOffset.X, (int) mOffset.Y, szX, szY);
            ImGui.SetCursorPos(cPos);
            //draw Points
            var pa = new Vector2(points.X0, points.Y0);
            var pb = new Vector2(points.X1, points.Y1);
            var pc = new Vector2(points.X2, points.Y2);
            drawList.AddTriangle((mOffset + pa * sz), (mOffset + pb * sz), (mOffset + pc * sz),
                    ColorInt(Color4.Magenta), 2);

            var mX = (int) (mainWindow.Mouse.X - mOffset.X);
            var mY = (int) (mainWindow.Mouse.Y - mOffset.Y);
            var pArray = new[] {pa, pb, pc};
            var pColors = new[] {Color4.Red, Color4.Green, Color4.Blue};
            var grabRadius = new Vector2(4,4);
            bool grabbed = false;
            //Pixel Grid
            if (pixels != null && pixels.Value.X >= 4 && pixels.Value.Y >= 4)
            {
                var px = pixels.Value;
                var amountX = 1f / px.X;
                var amountY = 1f / px.Y;
                for (int i = 0; i < (px.X - 1); i++) {
                    var lineX = amountX * (i + 1);
                    var pos = (mOffset + new Vector2(lineX, 0) * sz);
                    var pos2 = (mOffset + new Vector2(lineX, 1) * sz);
                    drawList.AddLine(pos, pos2, ColorInt(Color4.Gray, 0.4f));
                }
                for (int i = 0; i < (px.Y - 1); i++)
                {
                    var lineY = amountY * (i + 1);
                    var pos = (mOffset + new Vector2(0, lineY) * sz);
                    var pos2 = (mOffset + new Vector2(1, lineY) * sz);
                    drawList.AddLine(pos, pos2, ColorInt(Color4.Gray, 0.4f));
                }
            }
            //Edit Points
            float[] snaps = new[] {0.25f, 0.5f, 0.75f};
            for (int i = 0; i < pArray.Length; i++)
            {
                var pos = (mOffset + pArray[i] * sz);
                ImGui.SetCursorScreenPos(pos - new Vector2(2,2));
                drawList.AddCircleFilled(pos, grabRadius.X, ColorInt(pColors[i], 0.5f));
                ImGui.InvisibleButton($"##handle{i}", grabRadius * 2);
                if (ImGui.IsItemActive() || ImGui.IsItemHovered()) {
                    //Tooltip
                    ImGui.SetTooltip($"({pArray[i].X}, {pArray[i].Y})");
                    drawList.AddCircle(pos, grabRadius.X, ColorInt(Color4.Yellow));
                }
                else
                    drawList.AddCircle(pos, grabRadius.X, ColorInt(pColors[i]));

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0) && !grabbed)
                {
                    grabbed = true; //Solves overlapping points
                    pArray[i].X += ImGui.GetIO().MouseDelta.X / sz.X;
                    pArray[i].X = Snap(pArray[i].X, SNAP_TOLERANCE, snaps);
                    pArray[i].Y += ImGui.GetIO().MouseDelta.Y / sz.Y;
                    pArray[i].Y = Snap(pArray[i].Y, SNAP_TOLERANCE, snaps);
                }
            }
            ImGui.SetCursorScreenPos(mOffset + new Vector2(0, sz.Y));
            points.X0 = pArray[0].X;
            points.X1 = pArray[1].X;
            points.X2 = pArray[2].X;
            points.Y0 = pArray[0].Y;
            points.Y1 = pArray[1].Y;
            points.Y2 = pArray[2].Y;
            ImGui.PopID();
        }

        private const float SNAP_TOLERANCE = 0.005f;
        static float Snap(float a, float tolerance, float[] snaps)
        {
            a = MathHelper.Clamp(a, 0, 1);
            for (int i = 0; i < snaps.Length; i++) {
                if (Math.Abs(a - snaps[i]) < tolerance)
                {
                    a = snaps[i];
                    break;
                }
            }

            return a;
        }
        static uint ColorInt(Color4 c, float aMod = 1f)
        {
            return ImGui.ColorConvertFloat4ToU32(new Vector4(
                c.R, c.G, c.B, c.A * aMod
            ));
        }
    }
}