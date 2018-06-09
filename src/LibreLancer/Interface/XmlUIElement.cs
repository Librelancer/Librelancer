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
            var sz = CalculateSize();
            float h = sz.Y, w = sz.X;

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
            switch(Positioning.Anchor) {
                case XInt.Anchor.topleft:
                    return new Vector2(
                        r.X + (r.Height * Positioning.X),
                        r.Y + (r.Height * Positioning.Y)
                    );
                case XInt.Anchor.top:
                    return new Vector2(
                        r.X + (r.Width / 2) - (w / 2) + (r.Height * Positioning.X),
                        r.Y + (r.Height * Positioning.Y)
                    );
                case XInt.Anchor.topright:
                    return new Vector2(
                        r.X + r.Width - w - (r.Height * Positioning.X),
                        r.Y + (r.Height * Positioning.Y)
                    );
                case XInt.Anchor.bottomleft:
                    return new Vector2(
                        r.X + (r.Height * Positioning.X),
                        r.Y + r.Height - h - (r.Height * Positioning.Y)
                    );
                case XInt.Anchor.bottomright:
                    return new Vector2(
                        r.X + r.Width - w - (r.Height * Positioning.X),
                        r.Y + r.Height - h - (r.Height * Positioning.Y)
                    );
                case XInt.Anchor.bottom:
                    return new Vector2(
                        r.X + (r.Width / 2) - (w / 2) + (r.Height * Positioning.X),
                        r.Y + r.Height - h - (r.Height * Positioning.Y)
                    );
                default:
                    throw new Exception("Bad anchor");
            }
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
                e.Animation = new FlyInLeft(e.CalculatePosition(), start, duration) { From = -e.CalculateSize().X };
                e.Animation.Begin();
                e.Manager.AnimationFinishTimer = Math.Max(e.Manager.AnimationFinishTimer, start + duration);
            }
            public void flyout(float start, float duration)
            {
                e.Animation = new FlyOutLeft(e.CalculatePosition(), start, duration) { To = -e.CalculateSize().X };
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
        public virtual Vector2 CalculateSize() { return Vector2.Zero; }

    }
}
