// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
    
namespace LibreLancer
{
    public class XmlUITextBox : XmlUIPanel
    {
        public XInt.TextBox TextBox;

        public string CurrentText = "";
        public int MaxChars = 100;
        Font font;
        TextElement elem;

        public XmlUITextBox(XInt.TextBox text, XInt.Style style, XmlUIScene scn) : base(style, scn)
        {
            Positioning = text;
            ID = text.ID;
            TextBox = text;
            Lua = new LuaTextBox(this);
            font = scn.Manager.Game.Fonts.GetSystemFont("Arial Unicode MS");
            elem = Texts.Where((x) => x.ID == text.DisplayArea).First();
        }

        const double CARET_TIME = 0.5;
        double caretTime = 0.5;
        bool hasCaret = false;

        protected override void UpdateInternal(TimeSpan delta, bool updateInput)
        {
            base.UpdateInternal(delta, updateInput);
            if(!updateInput)
            {
                hasCaret = false;
                caretTime = CARET_TIME;
                return;
            }
            var p = CalculatePosition();
            var sz = CalculateSize();
            var container = new Rectangle((int)p.X, (int)p.Y, (int)sz.X, (int)sz.Y);
            if(Scene.MouseDown(MouseButtons.Left))
            {
                if(container.Contains(Scene.MouseX,Scene.MouseY))
                {
                    Scene.Focus = this;
                } else
                {
                    if (Scene.Focus == this) Scene.Focus = null;
                    hasCaret = false;
                    caretTime = CARET_TIME;
                    return;
                }
            }
            if(Scene.Focus == this)
            {
                caretTime -= delta.TotalSeconds;
                if(caretTime <= 0)
                {
                    hasCaret = !hasCaret;
                    caretTime = CARET_TIME;
                }
            }
        }

        public override void OnTextEntered(string s)
        {
            if(Scene.Focus == this) {
                CurrentText += s;
            }
        }

        public override void OnBackspace()
        {
            if(Scene.Focus == this) {
                if(CurrentText.Length > 0)
                {
                    CurrentText = CurrentText.Remove(CurrentText.Length - 1);
                }
            }
        }

        protected override void DrawInternal(TimeSpan delta)
        {
            base.DrawInternal(delta);
            var p = CalculatePosition();
            var sz = CalculateSize();
            var container = new Rectangle((int)p.X, (int)p.Y, (int)sz.X, (int)sz.Y);
            var r = elem.GetRectangle(container);
            var fontSize = TextElement.GetTextSize(r.Height);
            Scene.Renderer2D.Start(Scene.GWidth, Scene.GHeight);
            Scene.Renderer2D.DrawStringBaseline(font, fontSize, hasCaret ? CurrentText + "|" : CurrentText, r.X, r.Y, 0, elem.Style.Color);
            Scene.Renderer2D.Finish();
        }

        class LuaTextBox : LuaAPI
        {
            XmlUITextBox cb;
            public LuaTextBox(XmlUITextBox cb) : base(cb) => this.cb = cb;
            public void bordercolor(Color4 color) => cb.borderColor = color;
            public void focus() => cb.Scene.Focus = cb;
            public string gettext() => cb.CurrentText;
        }
    }
}
