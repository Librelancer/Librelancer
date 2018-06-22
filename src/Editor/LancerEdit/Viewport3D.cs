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
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;

namespace LibreLancer
{
    public class Viewport3D : IDisposable
    {
        RenderState rstate;
        ViewportManager vps;
        int rw = -1, rh = -1;
        int rid;
        RenderTarget2D renderTarget;
        public float Zoom = 200;
        public float ZoomStep = 0.25f;
        public Vector2 Rotation = Vector2.Zero;
        public int MarginH = 40;
        public int MarginW = 15;
        public int MinWidth = 120;
        public int MinHeight = 120;
        public Color4 Background = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);

        public int RenderWidth { get { return rw; }}
        public int RenderHeight { get { return rh; }}
        public Viewport3D(RenderState rstate, ViewportManager vps) 
        {
            this.rstate = rstate;
            this.vps = vps;
        }

        Color4 cc;
        public void Begin()
        {
            var renderWidth = Math.Max(120, (int)ImGui.GetWindowWidth() - MarginW);
            var renderHeight = Math.Max(120, (int)ImGui.GetWindowHeight() - MarginH);
            //Generate render target
            if (rh != renderHeight || rw != renderWidth)
            {
                if (renderTarget != null)
                {
                    ImGuiHelper.DeregisterTexture(renderTarget);
                    renderTarget.Dispose();
                }
                renderTarget = new RenderTarget2D(renderWidth, renderHeight);
                rid = ImGuiHelper.RegisterTexture(renderTarget);
                rw = renderWidth;
                rh = renderHeight;
            }
            cc = rstate.ClearColor;
            renderTarget.BindFramebuffer();
            vps.Push(0, 0, rw, rh);
            rstate.Cull = true;
            rstate.DepthEnabled = true;
            rstate.ClearColor = Background;
            rstate.ClearAll();
        }

        public void End()
        {
            vps.Pop();
            RenderTarget2D.ClearBinding();
            rstate.ClearColor = cc;
            rstate.DepthEnabled = false;
            rstate.BlendMode = BlendMode.Normal;
            rstate.Cull = false;
            //Viewport Control
            ImGui.ImageButton((IntPtr)rid, new Vector2(rw, rh),
                              Vector2.Zero, Vector2.One,
                              0,
                              Vector4.One, Vector4.One);
            if (ImGui.IsItemHovered(HoveredFlags.Default))
            {
                if (ImGui.IsMouseDragging(0, 1f))
                {
                    var delta = (Vector2)ImGui.GetMouseDragDelta(0, 1f);
                    Rotation -= (delta / 64);
                    ImGui.ResetMouseDragDelta(0);
                }
                float wheel = ImGui.GetIO().MouseWheel;
                if (ImGui.GetIO().ShiftPressed)
                    Zoom -= wheel * (2 * ZoomStep);
                else
                    Zoom -= wheel * ZoomStep;
                if (Zoom < 0) Zoom = 0;
            }
        }

        public void Dispose()
        {
            if(renderTarget != null) {
                ImGuiHelper.DeregisterTexture(renderTarget);
                renderTarget.Dispose();
            }
        }
    }
}
