// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Utf.Mat;
namespace LancerEdit
{
    public class TextureViewer : EditorTab
    {
        int tid;
        Texture2D tex;
        bool checkerboard = true;
        bool dispose;
        TexFrameAnimation anim;
        public TextureViewer(string title, Texture2D tex, TexFrameAnimation anim, bool disposeTex = true)
        {
            this.tex = tex;
            this.tid = ImGuiHelper.RegisterTexture(tex);
            Title = title;
            dispose = disposeTex;
            this.anim = anim;

        }

        int frame = 0;

        float zoom = 100;
        public override void Draw(double elapsed)
        {
            ImGui.Text("Zoom: ");
            ImGui.SameLine();
            ImGui.PushItemWidth(120);
            ImGui.SliderFloat("", ref zoom, 10, 800, "%.0f%%");
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (anim != null)
            {
                ImGui.PushItemWidth(80);
                ImGui.InputInt("Frame Number", ref frame, 1, 1);
                if (frame <= 0) frame = 0;
                if (frame >= anim.FrameCount) frame = anim.FrameCount - 1;
                ImGui.PopItemWidth();
                ImGui.SameLine();
            }
            ImGui.Checkbox("Checkerboard", ref checkerboard);
            ImGui.SameLine();

            bool doOpen = ImGui.Button("Info");
            if (doOpen)
                ImGui.OpenPopup("Info##" + Unique);
            ImGui.Separator();
            var w = ImGui.GetContentRegionAvail().X;
            zoom = (int)zoom;
            var scale = zoom / 100;
            var sz = new Vector2(tex.Width, tex.Height) * scale;
            bool isOpen = true;
            if (ImGui.BeginPopupModal("Info##" + Unique, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Format: " + tex.Format);
                ImGui.Text("Width: " + tex.Width);
                ImGui.Text("Height: " + tex.Height);
                ImGui.Text("Mipmaps: " + ((tex.LevelCount > 1) ? "Yes" : "No"));
                ImGui.EndPopup();
            }
            ImGuiNative.igSetNextWindowContentSize(new Vector2(sz.X, 0));
            ImGui.BeginChild("##scroll", new Vector2(-1), ImGuiChildFlags.Border, ImGuiWindowFlags.HorizontalScrollbar);
            var pos = ImGui.GetCursorScreenPos();
            var windowH = ImGui.GetWindowHeight();
            var windowW = ImGui.GetWindowWidth();
            var cbX = Math.Max(windowW, sz.X);
            var cbY = Math.Max(windowH, sz.Y);
            if (checkerboard)
            {
                unsafe
                {
                    var lst = ImGuiNative.igGetWindowDrawList();
                    ImGuiNative.ImDrawList_AddImage(lst, (IntPtr)ImGuiHelper.CheckerboardId,
                                                    pos, new Vector2(pos.X + cbX, pos.Y + cbY),
                                                    new Vector2(0, 0),
                                                    new Vector2(cbX / 16, cbY / 16),
                                                    uint.MaxValue);
                }
            }
            if (sz.Y < windowH) //Centre
            {
                ImGui.Dummy(new Vector2(5, (windowH / 2) - (sz.Y / 2)));
            }
            if (sz.X < w)
            {
                ImGui.Dummy(new Vector2((w / 2) - (sz.X / 2), 5));
                ImGui.SameLine();
            }
            var tl = new Vector2(0, 1);
            var br = new Vector2(1, 0);
            if(anim != null)
            {
                var f = anim.Frames[frame];
                tl = new Vector2(f.UV1.X, 1 - f.UV1.Y);
                br = new Vector2(f.UV2.X, 1 - f.UV2.Y);
            }
            ImGui.Image((IntPtr)tid, sz, tl,br,
                        Vector4.One, Vector4.Zero);

            ImGui.EndChild();
        }

        public override void Dispose()
        {
            if (dispose)
                tex.Dispose();
            ImGuiHelper.DeregisterTexture(tex);
        }
    }
}
