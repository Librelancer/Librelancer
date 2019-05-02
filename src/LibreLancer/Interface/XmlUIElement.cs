// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public class XmlUIElement
    {
        public string ID;
        public bool Visible = true;
        public XmlUIScene Scene;
        public UIAnimation Animation;
        public XInt.Positionable Positioning;


        public Vector2 CalculatePosition()
        {
            var r = new Rectangle(0, 0, Scene.GWidth, Scene.GHeight);
            var sz = CalculateSize();
            float h = sz.Y, w = sz.X;

            if (Positioning.Aspect == "4/3") {
                float scaleX = 1;
                float screenAspect = Scene.GWidth / (float)Scene.GHeight;
                float uiAspect = 4f / 3f;
                if (screenAspect > uiAspect)
                    scaleX = uiAspect / screenAspect;
                var newX = Scene.GWidth / 2f - (Scene.GWidth * scaleX / 2f);
                r.X = (int)newX;
                r.Width = (int)(Scene.GWidth * scaleX);
            }
            switch (Positioning.Anchor) {
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
        public XmlUIElement(XmlUIScene scene)
        {
            this.Scene = scene;
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
            public void setpos(float x, float y)
            {
                e.Positioning.X = x;
                e.Positioning.Y = y;
            }
            public void flyin(float start, float duration)
            {
                e.Animation = new FlyInLeft(e.CalculatePosition(), start, duration) { From = -e.CalculateSize().X };
                e.Animation.Begin();
                e.Scene.AnimationFinishTimer = Math.Max(e.Scene.AnimationFinishTimer, start + duration);
            }
            public void flyout(float start, float duration)
            {
                e.Animation = new FlyOutLeft(e.CalculatePosition(), start, duration) { To = -e.CalculateSize().X };
                e.Animation.Begin();
                e.Scene.AnimationFinishTimer = Math.Max(e.Scene.AnimationFinishTimer, start + duration);
            }
            public void hide()
            {
                e.Visible = false;
            }
            public void show()
            {
                e.Visible = true;
            }
        }
        public void Update(TimeSpan delta, bool updateInput)
        {
            if (Visible) UpdateInternal(delta, updateInput);
        }
        public void Draw(TimeSpan delta)
        {
            if (Visible) DrawInternal(delta);
        }
        protected virtual void UpdateInternal(TimeSpan delta, bool updateInput)
        {
            if (Animation != null && Animation.Running)
                Animation.Update(delta.TotalSeconds);
        }
        public virtual void OnMouseDown() { }
        public virtual void OnMouseUp() { }
        protected virtual void DrawInternal(TimeSpan delta) { }
        public virtual Vector2 CalculateSize() { return Vector2.Zero; }
        public virtual void OnTextEntered(string s) { }
        public virtual void OnBackspace() { }
    }
}
