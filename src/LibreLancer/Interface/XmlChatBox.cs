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

        public XmlChatBox(XInt.ChatBox chat, XInt.Style style, XmlUIManager man) : base(style,man)
        {
            Positioning = chat;
            ID = chat.ID;
            ChatBox = chat;
            Lua = new LuaChatBox(this);
            renderText = false;
            font = man.Game.Fonts.GetSystemFont("Arial Unicode MS");
            boldFont = man.Game.Fonts.GetSystemFont("Arial Unicode MS", FontStyles.Bold);
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
            var measured = Manager.Game.Renderer2D.MeasureString(boldFont, fontSize, CurrentEntry);
            Manager.Game.Renderer2D.Start(Manager.Game.Width, Manager.Game.Height);
            Manager.Game.Renderer2D.DrawWithClip(r, () =>
            {
                Manager.Game.Renderer2D.DrawStringBaseline(boldFont, fontSize, CurrentEntry, r.X + 3, r.Y + 1, r.X + 3, Color4.Black, false);
                Manager.Game.Renderer2D.DrawStringBaseline(boldFont, fontSize, CurrentEntry, r.X + 2, r.Y + 1, r.X + 2, elem.Style.Color, false);
                int a;
                int dY = 0;
                var str = string.Join("\n",
                                      Infocards.InfocardDisplay.WrapText(
                                          Manager.Game.Renderer2D,
                                          font,
                                          (int)fontSize,
                                          CurrentText,
                                          r.Width - 2,
                                          measured.X,
                                          out a,
                                          ref dY)
                                     );
                Manager.Game.Renderer2D.DrawStringBaseline(font, fontSize, str, r.X + 3 + measured.X, r.Y + 1, r.X + 3, Color4.Black, false);
                Manager.Game.Renderer2D.DrawStringBaseline(font, fontSize, str, r.X + 2 + measured.X, r.Y + 1, r.X + 2, elem.Style.Color, false);
            });
            Manager.Game.Renderer2D.Finish();
        }

        class LuaChatBox : LuaAPI
        {
            XmlChatBox cb;
            public LuaChatBox(XmlChatBox cb) : base(cb) => this.cb = cb;
            public void bordercolor(Color4 color) => cb.borderColor = color;
        }
    }
}
