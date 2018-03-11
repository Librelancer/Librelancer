/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using ImGuiNET;
using LibreLancer;
namespace LancerEdit
{
    public class TextureViewer : DockTab
    {
        int tid;
        Texture2D tex;
        bool open = true;
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
        public override bool Draw()
        {
            ImGui.Text("Zoom: ");
            ImGui.SameLine();
            ImGui.SliderFloat("", ref zoom, 10, 800, "%.0f%%", 1);
            ImGui.SameLine();
            ImGui.Checkbox("Checkerboard", ref checkerboard);
            ImGui.Separator();
            var w = ImGui.GetContentRegionAvailableWidth();
            zoom = (int)zoom;
            var scale = zoom / 100;
            var sz = new Vector2(tex.Width, tex.Height) * scale;
            ImGuiNative.igSetNextWindowContentSize(new Vector2(sz.X, 0));
            ImGui.BeginChild("##scroll", false, WindowFlags.HorizontalScrollbar);
            var pos = ImGui.GetCursorScreenPos();
            var windowH = ImGui.GetWindowHeight();
            var windowW = ImGui.GetWindowWidth();
            if (checkerboard)
            {
                unsafe
                {
                    var lst = ImGuiNative.igGetWindowDrawList();
                    ImGuiNative.ImDrawList_AddImage(lst, (void*)ImGuiHelper.CheckerboardId,
                                                    pos, new Vector2(pos.X + windowW, pos.Y + windowH),
                                                    new Vector2(0, 0),
                                                    new Vector2(windowW / 16, windowH / 16),
                                                    uint.MaxValue);
                }
            }
            if (sz.Y < windowH) //Centre
            {
                ImGui.Dummy(5, (windowH / 2) - (sz.Y / 2));
            }
            if (sz.X < w)
            {
                ImGui.Dummy((w / 2) - (sz.X / 2), 5);
                ImGui.SameLine();
            }
            ImGui.Image((IntPtr)tid, sz, new Vector2(0,1), new Vector2(1, 0),
                        Vector4.One, Vector4.Zero);
            ImGui.EndChild();
            return open;
        }

        public override void Dispose()
        {
            if (dispose)
                tex.Dispose();
            ImGuiHelper.DeregisterTexture(tex);
        }
    }
}
