// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public class TextElement
    {
        public string ID;
        public string Text;
        public Func<string> Binding;
        public XInt.StyleText Style;
        public Color4? ColorOverride;
        public LuaAPI Lua;
        public TextElement(XInt.StyleText style)
        {
            ID = style.ID;
            Style = style;
            Lua = new LuaAPI(this);
        }
        public Rectangle GetRectangle(Rectangle r)
        {
            var textR = new Rectangle(
                    (int)(r.X + r.Width * Style.X),
                    (int)(r.Y + r.Height * Style.Y),
                    (int)(r.Width * Style.Width),
                    (int)(r.Height * Style.Height)
                );
            return textR;
        }
        public void Draw(XmlUIManager manager, Rectangle r)
        {
            var s = Text;
            if(!string.IsNullOrEmpty(s)) {
                var textR = GetRectangle(r);
                if (Style.Background != null)
                {
                    manager.Game.Renderer2D.FillRectangle(textR, Style.Background.Value);
                }
                if (Style.Lines > 0)
                {
                    var tSize = (int)GetTextSize(textR.Height / (float)Style.Lines);
                    int a;
                    int dY = 0;
                    var str = string.Join("\n",
                                          Infocards.InfocardDisplay.WrapText(
                                              manager.Game.Renderer2D,
                                              manager.Font,
                                              tSize,
                                              s,
                                              textR.Width - 2,
                                              0,
                                              out a,
                                              ref dY)
                                         );
                    DrawShadowedText(manager, manager.Font, tSize, s, textR.X, textR.Y, ColorOverride ?? Style.Color, Style.Shadow);
                }
                else if(Style.Align == XInt.Align.Baseline)
                {
                    var sz = GetTextSize(textR.Height);
                    var size = manager.Game.Renderer2D.MeasureString(manager.Font, sz, s);
                    var pos = new Vector2(
                        textR.X + (textR.Width / 2f - size.X / 2),
                        textR.Y + (textR.Height / 2f - manager.Font.LineHeight(sz) / 2)
                    );
                    if(Style.Shadow != null) {
                        manager.Game.Renderer2D.DrawStringBaseline(manager.Font, sz, s, pos.X + 2, pos.Y + 2, pos.X, Style.Shadow.Value);
                    }
                    manager.Game.Renderer2D.DrawStringBaseline(manager.Font, sz, s, pos.X, pos.Y, pos.X, ColorOverride ?? Style.Color);
                }
                else
                {
                    DrawTextCentered(manager, manager.Font, GetTextSize(textR.Height), s, textR, ColorOverride ?? Style.Color, Style.Shadow);
                }
            }
            ColorOverride = null;
        }


        public class LuaAPI
        {
            TextElement elem;
            public LuaAPI(TextElement e)
            {
                elem = e;
            }
            public void value(string t)
            {
                elem.Text = t;
            }
            public void color(Color4 c)
            {
                elem.ColorOverride = c;
            }
        }

        public static float GetTextSize(float px)
        {
            return (int)Math.Floor((px * (72.0f / 96.0f)));
        }
        public static void DrawShadowedText(XmlUIManager m, Font font, float size, string text, float x, float y, Color4 c, Color4? s)
        {
            if (s != null)
                m.Game.Renderer2D.DrawString(font, size, text, x + 2, y + 2, s.Value);
            m.Game.Renderer2D.DrawString(font, size, text, x, y, c);
        }
        public static void DrawTextCentered(XmlUIManager m, Font font, float sz, string text, Rectangle rect, Color4 c, Color4? s)
        {
            var size = m.Game.Renderer2D.MeasureString(font, sz, text);
            var pos = new Vector2(
                rect.X + (rect.Width / 2f - size.X / 2),
                rect.Y + (rect.Height / 2f - size.Y / 2)
            );
            DrawShadowedText(m, font, sz, text, pos.X, pos.Y, c, s);
        }
    }
}
