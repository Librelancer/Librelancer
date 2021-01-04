// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Interface;
using LibreLancer.GameData;
using LibreLancer.ImUI;

namespace SystemViewer
{
    public class SystemMap
    {
        private UiContext ctx;
        private Navmap navmap;
        private MainWindow win;
        public void CreateContext(MainWindow window)
        {
            var uidata = new UiData();
            uidata.FileSystem = window.GameData.VFS;
            uidata.Fonts = window.GetService<FontManager>();
            uidata.ResourceManager = window.Resources;
            ctx = new UiContext(uidata);
            ctx.RenderState = window.RenderState;
            ctx.Renderer2D = window.Renderer2D;
            navmap = new Navmap();
            navmap.Width = 480;
            navmap.Height = 480;
            navmap.LetterMargin = true;
            navmap.MapBorder = true;
            ctx.SetWidget(navmap);
            this.win = window;
        }

        public void SetObjects(StarSystem sys)
        {
            navmap.PopulateIcons(ctx, sys);
        }

        private RenderTarget2D rtarget;
        private int rw = -1, rh = -1, rt = -1;
        public void Draw(int width, int height, TimeSpan delta)
        {
            //Set viewport
            height -= 30;
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;
            if (width != rw || height != rh)
            {
                if (rtarget != null) {
                    ImGuiHelper.DeregisterTexture(rtarget.Texture);
                    rtarget.Dispose();
                }
                rtarget = new RenderTarget2D(width, height);
                rw = width;
                rh = height;
                rt = ImGuiHelper.RegisterTexture(rtarget.Texture);
            }
            //Draw
            win.Viewport.Push(0, 0, width, height);
            ctx.ViewportWidth = width;
            ctx.ViewportHeight = height;
            ctx.RenderState.RenderTarget = rtarget;
            ctx.RenderState.ClearColor = Color4.TransparentBlack;
            ctx.RenderState.ClearAll();
            ctx.RenderWidget(delta);
            ctx.RenderState.RenderTarget = null;
            win.Viewport.Pop();
            //ImGui
            ImGui.Button("x##a");
            ImGui.SameLine();
            ImGui.Button("x##b");
            var cpos = ImGui.GetCursorPos();
            ImGui.Image((IntPtr)rt, new Vector2(width, height), new Vector2(0,1), new Vector2(1,0),
            Color4.White);
            ImGui.SetCursorPos(cpos);
            ImGui.InvisibleButton("##navmap", new Vector2(width, height));
        }
    }
}