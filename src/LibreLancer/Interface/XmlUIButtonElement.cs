// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
            if(Texts.Count > 0 && !Enabled) {
                Texts[0].Text = Button.Text;
                Texts[0].ColorOverride = Color4.Gray;
            }
            if ((Animation != null && Animation.Running) || !Enabled) return;

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
