// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.GameData.World;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Interface;

namespace LancerEdit.GameContent
{
    public class SystemMap
    {
        private UiContext ctx;
        public Navmap Control;
        private MainWindow win;
        public void CreateContext(GameDataContext context, MainWindow win)
        {
            var uidata = new UiData();
            uidata.FileSystem = context.GameData.VFS;
            uidata.DataPath = context.GameData.Items.Ini.Freelancer.DataPath;
            uidata.Infocards = context.GameData.Items.Ini.Infocards;
            if (context.GameData.Items.Ini.Navmap != null)
                uidata.NavmapIcons = new IniNavmapIcons(context.GameData.Items.Ini.Navmap);
            else
                uidata.NavmapIcons = new NavmapIcons();
            uidata.Fonts = context.Fonts;
            uidata.ResourceManager = context.Resources;
            ctx = new UiContext(uidata);
            ctx.RenderContext = win.RenderContext;
            Control = new Navmap();
            Control.Width = 480;
            Control.Height = 480;
            Control.LetterMargin = true;
            Control.MapBorder = true;
            ctx.SetWidget(Control);
            this.win = win;
        }

        public void SetObjects(StarSystem sys)
        {
            Control.PopulateIcons(ctx, sys);
        }

        private RenderTarget2D rtarget;
        private int rw = -1, rh = -1;
        private ImTextureRef rt;

        static bool NavButton(char icon, string tooltip, bool selected)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            if (selected) {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            }
            var ret = ImGui.Button(icon);
            if(selected) ImGui.PopStyleColor();
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip(tooltip);
            }
            ImGui.PopStyleVar();
            return ret;
        }
        public void Draw(int width, int height, double delta)
        {
            //Set viewport
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;
            if (width != rw || height != rh)
            {
                if (rtarget != null) {
                    ImGuiHelper.DeregisterTexture(rtarget.Texture);
                    rtarget.Dispose();
                }
                rtarget = new RenderTarget2D(ctx.RenderContext, width, height);
                rw = width;
                rh = height;
                rt = ImGuiHelper.RegisterTexture(rtarget.Texture);
            }
            //Draw
            win.RenderContext.PushViewport(0, 0, width, height);
            ctx.ViewportWidth = width;
            ctx.ViewportHeight = height;
            ctx.RenderContext.RenderTarget = rtarget;
            ctx.RenderContext.ClearColor = Color4.TransparentBlack;
            ctx.RenderContext.ClearAll();
            ctx.RenderWidget(delta);
            ctx.RenderContext.RenderTarget = null;
            win.RenderContext.PopViewport();
            //ImGui
            //TODO: Implement in Navmap then add buttons
            /*
            NavButton("nav_labels", "Show Labels", true);
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(72, 16)); //padding
            ImGui.SameLine();
            NavButton("nav_physical", "Physical Map", false);
            ImGui.SameLine();
            NavButton("nav_political", "Political Map", false);
            ImGui.SameLine();
            NavButton("nav_patrol", "Patrol Paths", false);
            ImGui.SameLine();
            NavButton("nav_mining", "Mining Zones", false);
            ImGui.SameLine();
            NavButton("nav_legend", "Legend", false);
            ImGui.SameLine();
            NavButton("nav_knownbases", "Known Bases", false);
            */
            var cpos = ImGui.GetCursorPos();
            ImGui.Image(rt, new Vector2(width, height), new Vector2(0, 1), new Vector2(1, 0));
            ImGui.SetCursorPos(cpos);
            ImGui.InvisibleButton("##navmap", new Vector2(width, height));
        }

        public void Dispose()
        {
            if (rtarget != null) {
                ImGuiHelper.DeregisterTexture(rtarget.Texture);
                rtarget.Dispose();
                rtarget = null;
            }
        }
    }
}
