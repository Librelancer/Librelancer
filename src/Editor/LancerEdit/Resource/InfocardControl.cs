// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Infocards;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;

namespace LancerEdit
{
    public class InfocardControl : IDisposable
    {
        BuiltRichText icard;
        MainWindow window;
        RenderTarget2D renderTarget;
        int renderWidth = -1, renderHeight = -1, rid = -1;
        public string InfocardText { get; private set; }
        public InfocardControl(MainWindow win, Infocard infocard, float initWidth)
        {
            window = win;
            icard = win.RichText.BuildText(infocard.Nodes, (int)initWidth, 0.7f * ImGuiHelper.Scale);
        }
        public void SetInfocard(Infocard infocard)
        {
            icard.Dispose();
            InfocardText = infocard.ExtractText();
            icard = window.RichText.BuildText(infocard.Nodes, renderWidth > 0 ? renderWidth : 400, 0.7f * ImGuiHelper.Scale);
        }
        public void Draw(float width)
        {
            icard.Recalculate(width);
            if (icard.Height < 1 || width < 1) {
                ImGui.Dummy(new Vector2(1, 1));
                return;
            }
            var cPos = (Vector2)ImGui.GetCursorPos();
            var wPos = (Vector2)ImGui.GetWindowPos();
            var scrPos = -ImGui.GetScrollY();
            var mOffset = cPos + wPos + new Vector2(0, scrPos);
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddImage((IntPtr)rid,
                new Vector2((int)mOffset.X, (int)mOffset.Y),
                new Vector2((int)(mOffset.X + renderWidth), (int)(mOffset.Y + icard.Height)),
                new Vector2(0, 1), new Vector2(1, 0));

            drawList.AddCallback(IntPtr.MaxValue, ImGuiHelper.Callback(s =>
            {
                window.RenderContext.ScissorEnabled = true;
                window.RenderContext.ScissorRectangle = s;
                window.RichText.RenderText(icard, (int)mOffset.X, (int)mOffset.Y);
                window.RenderContext.ScissorEnabled = false;
            }));

            ImGui.InvisibleButton("##infocardbutton", new System.Numerics.Vector2(renderWidth, icard.Height));
        }
        public void Dispose()
        {
            icard?.Dispose();
        }
    }
}
