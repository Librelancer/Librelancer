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
namespace LibreLancer
{
    public class XmlUIButton : XmlUIPanel
    {
        public XInt.Button Button;

        ModifiedStyle hoverStyle = new ModifiedStyle();
        Neo.IronLua.LuaChunk hoverChunk;
        class ModifiedStyle
        {
            public Color4? ModelColor;
            public Color4? TextColor;

            public void modelcolor(Color4 c) => ModelColor = c;
            public void textcolor(Color4 c) => TextColor = c;

            public void Reset()
            {
                ModelColor = null;
                TextColor = null;
            }
        }

        public XmlUIButton(XmlUIManager manager, XInt.Button button, XInt.Style style) : base(style,manager)
        {
            Button = button;
            Positioning = button;
            if (Style.HoverStyle != null)
            {
                hoverChunk = LuaStyleEnvironment.L.CompileChunk(
                    style.HoverStyle, "buttonHover", new Neo.IronLua.LuaCompileOptions()
                );
            }

            ID = button.ID;
        }
        
        bool lastDown = false;
        protected override void UpdateInternal(TimeSpan delta)
        {
            base.UpdateInternal(delta);
            if (Animation != null && Animation.Running) return;

            var h = Manager.Game.Height * Style.Size.Height;
            var w = h * Style.Size.Ratio;
            var pos = CalculatePosition();
            var r = new Rectangle((int)pos.X, (int)pos.Y, (int)w, (int)h);
            hoverStyle.Reset();
            if (r.Contains(Manager.Game.Mouse.X, Manager.Game.Mouse.Y))
            {
                if (!lastDown && Manager.Game.Mouse.IsButtonDown(MouseButtons.Left))
                {
                    if (!string.IsNullOrEmpty(Button.OnClick))
                        Manager.Call(Button.OnClick);
                }
                if(hoverChunk != null)
                    LuaStyleEnvironment.Do(hoverChunk, hoverStyle, (float)Manager.Game.TotalTime);
            }
            modelColor = hoverStyle.ModelColor;
            lastDown = Manager.Game.Mouse.IsButtonDown(MouseButtons.Left);
        }
        protected override void DrawInternal(TimeSpan delta)
        {
            base.DrawInternal(delta);
            var h = Manager.Game.Height * Style.Size.Height;
            var w = h * Style.Size.Ratio;
            var pos = CalculatePosition();
            int px = (int)pos.X, py = (int)pos.Y;
            if (Animation != null && (Animation.Remain || Animation.Running))
            {
                px = (int)Animation.CurrentPosition.X;
                py = (int)Animation.CurrentPosition.Y;
            }
            var r = new Rectangle(px, py, (int)w, (int)h);
            //Draw Text
            if (!string.IsNullOrEmpty(Button.Text) && Style.Text != null)
            {
                var t = Style.Text;
                var textR = new Rectangle(
                    (int)(r.X + r.Width * t.X),
                    (int)(r.Y + r.Height * t.Y),
                    (int)(r.Width * t.Width),
                    (int)(r.Height * t.Height)
                );
                Manager.Game.Renderer2D.Start(Manager.Game.Width, Manager.Game.Height);
                if (t.Background != null)
                {
                    Manager.Game.Renderer2D.FillRectangle(textR, t.Background.Value);
                }
                DrawTextCentered(Manager.Font, GetTextSize(textR.Height), Button.Text, textR, hoverStyle.TextColor ?? t.Color, t.Shadow);
                Manager.Game.Renderer2D.Finish();
            }
        }
    }
}
