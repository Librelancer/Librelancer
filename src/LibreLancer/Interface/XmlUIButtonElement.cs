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
            public Vector3 Rotation;
            public void modelcolor(Color4 c) => ModelColor = c;
            public void modelrotate(float x, float y, float z) => Rotation = new Vector3(x, y, z);
            public void textcolor(Color4 c) => TextColor = c;
           
            public void Reset()
            {
                ModelColor = null;
                TextColor = null;
                Rotation = Vector3.Zero;
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
            modelRotate = hoverStyle.Rotation;
            lastDown = Manager.Game.Mouse.IsButtonDown(MouseButtons.Left);
            if(Texts.Count > 0) {
                Texts[0].Text = Button.Text;
                Texts[0].ColorOverride = hoverStyle.TextColor;
            }
        }
    }
}
