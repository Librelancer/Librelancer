// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;

namespace LibreLancer.Interface
{
    public class UiContext
    {
        //State
        public float ViewportWidth;
        public float ViewportHeight;
        public float MouseX;
        public float MouseY;
        public bool MouseLeftDown;
        public double GlobalTime;
        //Rendering
        public RenderState RenderState;
        public Renderer2D Renderer2D;
        public MatrixCamera MatrixCam = new MatrixCamera(Matrix4x4.Identity);
        //Data
        public UiData Data;
        //Ui
        private object _gameApi;
        public object GameApi
        {
            get
            {
                return _gameApi;
            }
            set
            {
                _gameApi = value;
                lua?.SetGameApi(_gameApi);
            }
        }
        LuaContext lua;
        //State
        private bool mode2d = false;
        private FreelancerGame game;
        public UiContext(UiData data)
        {
            Data = data;
        }
        
        public UiContext(FreelancerGame game)
        {
            Renderer2D = game.Renderer2D;
            RenderState = game.RenderState;
            Data = new UiData(game);
            this.game = game;
            game.Mouse.MouseDown += MouseOnMouseDown;
            game.Mouse.MouseUp += MouseOnMouseUp;
        }

        public void LoadCode()
        {
            lua = new LuaContext(this);
            lua.LoadMain();
        }

        public void OpenScene(string scene)
        {
            lua.OpenScene(scene);
        }
        
        
        
        private void MouseOnMouseUp(MouseEventArgs e)
        {
            if (game.Debug.CaptureMouse) return;
            if ((e.Buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                OnMouseClick();
                OnMouseUp();
            }
        }

        private void MouseOnMouseDown(MouseEventArgs e)
        {
            if (game.Debug.CaptureMouse) return;
            if ((e.Buttons & MouseButtons.Left) == MouseButtons.Left) OnMouseDown();
        }

        public Vector2 AnchorPosition(RectangleF parent, AnchorKind anchor, float x, float y, float width, float height)
        {
            float resolveX = 0;
            float resolveY = 0;
            switch (anchor)
            {
                case AnchorKind.TopLeft:
                    resolveX = parent.X + x;
                    resolveY = parent.Y + y;
                    break;
                case AnchorKind.TopCenter:
                    resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                    resolveY = parent.Y + y;
                    break;
                case AnchorKind.TopRight:
                    resolveX = parent.X + parent.Width - width - x;
                    resolveY = parent.Y + y;
                    break;
                case AnchorKind.CenterLeft:
                    resolveX = parent.X + x;
                    resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                    break;
                case AnchorKind.Center:
                    resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                    resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                    break;
                case AnchorKind.CenterRight:
                    resolveX = parent.X + parent.Width - width - x;
                    resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                    break;
                case AnchorKind.BottomLeft:
                    resolveX = parent.X + x;
                    resolveY = parent.Y + parent.Height - height - y;
                    break;
                case AnchorKind.BottomCenter:
                    resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                    resolveY = parent.Y + parent.Height - height - y;
                    break;
                case AnchorKind.BottomRight:
                    resolveX = parent.X + parent.Width - width - x;
                    resolveY = parent.Y + parent.Height - height - y;
                    break;
            }
            return new Vector2(resolveX, resolveY);
        }
        
        public Vector2 PointsToPixels(Vector2 points)
        {
            var ratio = ViewportHeight / 480;
            return points * ratio;
        }

        public float PixelsToPoints(float pixels)
        {
            var ratio = 480 / ViewportHeight;
            return pixels * ratio;
        }

        public int PointsToPixels(float points)
        {
            var ratio = ViewportHeight / 480;
            var i = (int) (points * ratio);
            return i <= 0 ? 1 : i;
        }

        public RectangleF PointsToPixelsF(RectangleF points)
        {
            var ratio = ViewportHeight / 480;
            return new RectangleF(points.X * ratio, points.Y * ratio, points.Width * ratio, points.Height * ratio);
        }

        public Vector2 PointsToPixelsF(Vector2 points)
        {
            var ratio = ViewportHeight / 480;
            return ratio * points;
        }
        public Rectangle PointsToPixels(RectangleF points)
        {
            var ratio = ViewportHeight / 480;
            return new Rectangle((int)(points.X * ratio), (int)(points.Y * ratio), (int)(points.Width * ratio), (int)(points.Height * ratio));
        }
        public float TextSize(float inputPoints)
        {
            var ratio = ViewportHeight / 480;
            var pixels = inputPoints * ratio;
            return (int)Math.Floor(pixels);
        }
        
        public void Mode2D()
        {
            if (mode2d) return;
            Renderer2D.Start((int)ViewportWidth, (int)ViewportHeight);
            mode2d = true;
        }
        
        public void Mode3D()
        {
            if (!mode2d) return;
            Renderer2D.Finish();
            mode2d = false;
        }
        
        public void Update(UiWidget widget, double globalTime, int mouseX, int mouseY, bool leftDown)
        {
            GlobalTime = globalTime;
            var inputRatio = 480 / ViewportHeight;
            MouseX = mouseX * inputRatio;
            MouseY = mouseY * inputRatio;
            MouseLeftDown = leftDown;
            lua?.DoTimers(globalTime);
            lua?.CallEvent("Update", globalTime);
        }

        public void Update(FreelancerGame game)
        {
            ViewportWidth = game.Width;
            ViewportHeight = game.Height;
            if(Visible)
            Update(baseWidget, game.TotalTime,
                game.Mouse.X, game.Mouse.Y, game.Mouse.IsButtonDown(MouseButtons.Left));
        }

        public bool MouseWanted(int mouseX, int mouseY)
        {
            var inputRatio = 480 / ViewportHeight;
            if (!Visible) return false;
            if (modals.Count > 0) return true;
            return baseWidget?.MouseWanted(this, GetRectangle(), mouseX * inputRatio, mouseY * inputRatio) ?? false;
        }

        public void OnFocus()
        {
            baseWidget.UnFocus();
        }

        public void Dispose()
        {
            baseWidget = null;
        }
        RectangleF GetRectangle() => new RectangleF(0,0, 480 * (ViewportWidth / ViewportHeight), 480);

        private UiWidget baseWidget;
        List<ModalState> modals = new List<ModalState>();
        public void SetWidget(UiWidget widget)
        {
            widget.ApplyStylesheet(Data.Stylesheet);
            foreach (var m in modals)
                m.Widget.Dispose();
            modals = new List<ModalState>();
            baseWidget = widget;
        }

        private int _h = 0;
        public int OpenModal(UiWidget widget)
        {
            var handle = _h++;
            widget.ApplyStylesheet(Data.Stylesheet);
            modals.Add(new ModalState() {Widget = widget, Handle = handle});
            return handle;
        }

        public void SwapModal(UiWidget widget, int handle)
        {
            widget.ApplyStylesheet(Data.Stylesheet);
            for (int i = 0; i < modals.Count; i++)
            {
                if (modals[i].Handle == handle)
                {
                    modals[i].Widget = widget;
                    break;
                }
            }
        }

        public void CloseModal(int handle)
        {
            for (int i = 0; i < modals.Count; i++)
            {
                if (modals[i].Handle == handle)
                {
                    modals.RemoveAt(i);
                    break;
                }
            }
        }

        public void PlaySound(string sound)
        {
            Data.Sounds?.PlayOneShot(sound);
        }
        
        class ModalState
        {
            public UiWidget Widget;
            public int Handle;
        }

        public bool Visible = true;
        UiWidget GetActive()
        {
            if(!Visible) return null;
            if (baseWidget == null) return null;
            if (modals.Count > 0) return modals[modals.Count - 1].Widget;
            return baseWidget;
        }

        public void ChatboxEvent() => Event("Chatbox");
        public void Event(string ev)
        {
            lua.CallEvent(ev);
        }
        public void OnMouseDown() => GetActive()?.OnMouseDown(this, GetRectangle());
        public void OnMouseUp() => GetActive()?.OnMouseUp(this, GetRectangle());
        public void OnMouseClick() => GetActive()?.OnMouseClick(this, GetRectangle());

        public void OnMouseWheel(float delta) => GetActive()?.OnMouseWheel(this, GetRectangle(), delta);

        private UiWidget textFocusWidget = null;

        internal void SetTextFocus(UiWidget widget) => textFocusWidget = widget;

        public bool KeyboardGrabbed => textFocusWidget != null;

        public void OnKeyDown(Keys key) => textFocusWidget?.OnKeyDown(key);

        public void OnTextEntry(string text) => textFocusWidget?.OnTextInput(text);

        public double DeltaTime;
        public void RenderWidget(double delta)
        {
            DeltaTime = delta;
            if (baseWidget == null || !Visible)
            {
                textFocusWidget = null;
                return;
            }
            if (game != null && game.Mouse.Wheel != 0)
            {
                OnMouseWheel(game.Mouse.Wheel);   
            }
            textFocusWidget = null;
            RenderState.DepthEnabled = false;
            mode2d = false;
            var aspect = ViewportWidth / ViewportHeight;
            var desktopRect = new RectangleF(0, 0, 480 * aspect, 480);
            baseWidget.Render(this, desktopRect);
            foreach(var widget in modals)
                widget.Widget.Render(this, desktopRect);
            if (mode2d)
                Renderer2D.Finish();
            RenderState.DepthEnabled = true;
        }
    }
}