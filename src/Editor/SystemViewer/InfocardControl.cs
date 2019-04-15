// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Infocards;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;
namespace SystemViewer
{
    public class InfocardControl : IDisposable
    {
        InfocardDisplay icard;
        MainWindow window;
        RenderTarget2D renderTarget;
        int renderWidth = -1, renderHeight = -1, rid = -1;
        public InfocardControl(MainWindow win, Infocard infocard, float initWidth)
        {
            window = win;
            icard = new InfocardDisplay(win, new Rectangle(0, 0, (int)initWidth, int.MaxValue), infocard);
        }
        public void SetInfocard(Infocard infocard)
        {
            icard.SetInfocard(infocard);
        }
        public void Draw(float width)
        {
            icard.SetRectangle(new Rectangle(0, 0, (int)width, int.MaxValue));

            if (icard.Height != renderHeight || (int)width != renderWidth)
            {
                renderWidth = (int)width;
                renderHeight = (int)icard.Height;
                if (renderTarget != null)
                {
                    ImGuiHelper.DeregisterTexture(renderTarget);
                    renderTarget.Dispose();
                }
                renderTarget = new RenderTarget2D(renderWidth, renderHeight);
                rid = ImGuiHelper.RegisterTexture(renderTarget);
            }
            renderTarget.BindFramebuffer();
            window.Viewport.Push(0, 0, renderWidth, renderHeight);
            var cc = window.RenderState.ClearColor;
            window.RenderState.ClearColor = Color4.Transparent;
            window.RenderState.ClearAll();
            window.RenderState.ClearColor = cc;
            window.Renderer2D.Start(renderWidth, renderHeight);
            icard.Draw(window.Renderer2D);
            window.Renderer2D.Finish();
            RenderTarget2D.ClearBinding();
            window.Viewport.Pop();

            //ImGui. Base off ImageButton so we can get input for selection later
            var style = ImGui.GetStyle();
            var btn = style.Colors[(int)ImGuiCol.Button];
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, btn);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, btn);
            ImGui.ImageButton((IntPtr)rid, new Vector2(renderWidth, icard.Height),
                                 new Vector2(0, 1), new Vector2(1, 0),
                                 0,
                                 Vector4.Zero, Vector4.One);
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            //Somehow keep track of selection? (idk if InfocardDisplay should do this)
        }
        public void Dispose()
        {
            renderTarget.Dispose();
        }
    }
}
