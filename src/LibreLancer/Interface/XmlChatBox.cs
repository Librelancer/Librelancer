// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;

namespace LibreLancer
{
    public class XmlChatBox : XmlUIPanel
    {
        public XInt.ChatBox ChatBox;

        public string CurrentEntry = "Console->";
        public string CurrentText = "";
        public int MaxChars = 100;
        Font font;
        Font boldFont;
        TextElement elem;

        public XmlChatBox(XInt.ChatBox chat, XInt.Style style, XmlUIScene scn) : base(style,scn)
        {
            Positioning = chat;
            ID = chat.ID;
            ChatBox = chat;
            Lua = new LuaChatBox(this);
            renderText = false;
            font = scn.Manager.Game.Fonts.GetSystemFont("Arial Unicode MS");
            boldFont = scn.Manager.Game.Fonts.GetSystemFont("Arial Unicode MS", FontStyles.Bold);
            elem = Texts.Where((x) => x.ID == chat.DisplayArea).First();
            Visible = false;
        }

        public bool AppendText(string str)
        {
            if (CurrentText.Length + str.Length > MaxChars)
                return false;
            CurrentText += str;
            return true;
        }

        protected override void DrawInternal(TimeSpan delta)
        {
            base.DrawInternal(delta);
            var p = CalculatePosition();
            var sz = CalculateSize();
            var container = new Rectangle((int)p.X, (int)p.Y, (int)sz.X, (int)sz.Y);
            var r = elem.GetRectangle(container);
            var fontSize = TextElement.GetTextSize(r.Height / 3.2f);
            var measured =  Scene.Renderer2D.MeasureString(boldFont, fontSize, CurrentEntry);
            Scene.Renderer2D.Start(Scene.GWidth, Scene.GHeight);
            Scene.Renderer2D.DrawWithClip(r, () =>
            {
                Scene.Renderer2D.DrawStringBaseline(boldFont, fontSize, CurrentEntry, r.X + 3, r.Y + 1, r.X + 3, Color4.Black, false);
                Scene.Renderer2D.DrawStringBaseline(boldFont, fontSize, CurrentEntry, r.X + 2, r.Y + 1, r.X + 2, elem.Style.Color, false);
                int a;
                int dY = 0;
                var str = string.Join("\n",
                                      TextUtils.WrapText(
                                            Scene.Renderer2D,
                                          font,
                                          (int)fontSize,
                                          CurrentText,
                                          r.Width - 2,
                                          measured.X,
                                          out a,
                                          ref dY)
                                     );
                Scene.Renderer2D.DrawStringBaseline(font, fontSize, str, r.X + 3 + measured.X, r.Y + 1, r.X + 3, Color4.Black, false);
                Scene.Renderer2D.DrawStringBaseline(font, fontSize, str, r.X + 2 + measured.X, r.Y + 1, r.X + 2, elem.Style.Color, false);
            });
            Scene.Renderer2D.Finish();
        }

        class LuaChatBox : LuaAPI
        {
            XmlChatBox cb;
            public LuaChatBox(XmlChatBox cb) : base(cb) => this.cb = cb;
            public void bordercolor(Color4 color) => cb.borderColor = color;
        }
    }
}
