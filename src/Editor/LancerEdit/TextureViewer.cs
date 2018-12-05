// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
namespace LancerEdit
{
    public class TextureViewer : EditorTab
    {
        int tid;
        Texture2D tex;
        bool checkerboard = true;
        bool dispose;
        public TextureViewer(string title, Texture2D tex, bool disposeTex = true)
        {
            this.tex = tex;
            this.tid = ImGuiHelper.RegisterTexture(tex);
            Title = title;
            dispose = disposeTex;
        }
        float zoom = 100;
        public override void Draw()
        {
            ImGui.Text("Zoom: ");
            ImGui.SameLine();
            ImGui.SliderFloat("", ref zoom, 10, 800, "%.0f%%", 1);
            ImGui.SameLine();
            ImGui.Checkbox("Checkerboard", ref checkerboard);
            ImGui.SameLine();
            bool doOpen = ImGui.Button("Info");
            if (doOpen)
                ImGui.OpenPopup("Info##" + Unique);
            ImGui.Separator();
            var w = ImGui.GetContentRegionAvailWidth();
            zoom = (int)zoom;
            var scale = zoom / 100;
            var sz = new Vector2(tex.Width, tex.Height) * scale;
            ImGuiNative.igSetNextWindowContentSize(new Vector2(sz.X, 0));
            bool isOpen = true;
            if (ImGui.BeginPopupModal("Info##" + Unique, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Format: " + tex.Format);
                ImGui.Text("Width: " + tex.Width);
                ImGui.Text("Height: " + tex.Height);
                ImGui.Text("Mipmaps: " + ((tex.LevelCount > 1) ? "Yes" : "No"));
                ImGui.EndPopup();
            }
            ImGui.BeginChild("##scroll", new Vector2(-1), false, ImGuiWindowFlags.HorizontalScrollbar);
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
            ImGui.Image((IntPtr)tid, sz, new Vector2(0,1), new Vector2(1, 0),
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
