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
    public class XmlUIElement
    {
        public string ID;
        public bool Visible = true;
        public XmlUIManager Manager;
        public UIAnimation Animation;
        public XInt.Positionable Positioning;

        public Vector2 CalculatePosition()
        {
            var r = new Rectangle(0, 0, Manager.Game.Width, Manager.Game.Height);
            if(Positioning.Aspect == "4/3") {
                float scaleX = 1;
                float screenAspect = Manager.Game.Width / (float)Manager.Game.Height;
                float uiAspect = 4f / 3f;
                if (screenAspect > uiAspect)
                    scaleX = uiAspect / screenAspect;
                var newX = Manager.Game.Width / 2f - (Manager.Game.Width * scaleX / 2f);
                r.X = (int)newX;
                r.Width = (int)(Manager.Game.Width * scaleX);
            }
            return new Vector2(
                r.X + (r.Width * Positioning.X),
                r.Y + (r.Height * Positioning.Y)
            );
        }
        public XmlUIElement(XmlUIManager manager)
        {
            this.Manager = manager;
            Lua = new LuaAPI(this);
        }
        public LuaAPI Lua;
        public class LuaAPI
        {
            XmlUIElement e;
            public LuaAPI(XmlUIElement e)
            {
                this.e = e;
            }
            public void flyin(float start, float duration)
            {
                e.Animation = new FlyInLeft(e.CalculatePosition(), start, duration) { From = -e.CalculateWidth() };
                e.Animation.Begin();
                e.Manager.AnimationFinishTimer = Math.Max(e.Manager.AnimationFinishTimer, start + duration);
            }
            public void flyout(float start, float duration)
            {
                e.Animation = new FlyOutLeft(e.CalculatePosition(), start, duration) { To = -e.CalculateWidth() };
                e.Animation.Begin();
                e.Manager.AnimationFinishTimer = Math.Max(e.Manager.AnimationFinishTimer, start + duration);
            }
            public void hide()
            {
                e.Visible = false;
            }
        }
        public void Update(TimeSpan delta)
        {
            if (Visible) UpdateInternal(delta);
        }
        public void Draw(TimeSpan delta)
        {
            if (Visible) DrawInternal(delta);
        }
        protected virtual void UpdateInternal(TimeSpan delta)
        {
            if (Animation != null && Animation.Running)
                Animation.Update(delta.TotalSeconds);
        }
        protected virtual void DrawInternal(TimeSpan delta) { }
        public virtual float CalculateWidth() { return 0; }
        protected float GetTextSize(float px)
        {
            return (int)Math.Floor((px * (72.0f / 96.0f)));
        }
        protected void DrawShadowedText(Font font, float size, string text, float x, float y, Color4 c, Color4 s)
        {
            Manager.Game.Renderer2D.DrawString(font, size, text, x + 2, y + 2, s);
            Manager.Game.Renderer2D.DrawString(font, size, text, x, y, c);
        }
        protected void DrawTextCentered(Font font, float sz, string text, Rectangle rect, Color4 c, Color4? s)
        {
            var size = Manager.Game.Renderer2D.MeasureString(font, sz, text);
            var pos = new Vector2(
                rect.X + (rect.Width / 2f - size.X / 2),
                rect.Y + (rect.Height / 2f - size.Y / 2)
            );
            if (s != null)
                DrawShadowedText(font, sz, text, pos.X, pos.Y, c, s.Value);
            else
                Manager.Game.Renderer2D.DrawString(font, sz, text, pos.X, pos.Y, c);
        }

    }
}
