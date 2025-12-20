// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Graphics;
using LibreLancer.Render;

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

        public float ScreenWidth => 480 * (ViewportWidth / ViewportHeight);
        //Rendering
        public RenderContext RenderContext;
        public LineRenderer Lines;
        //Data
        public UiData Data;

        public CommandBuffer CommandBuffer;
        //Ui
        private object _gameApi;
        //TODO: Properly reload RigidModels on meshes cleared
        public int MeshDisposeVersion = 0;
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
            RenderContext = game.RenderContext;
            Lines = game.Lines;
            Data = new UiData(game);
            this.game = game;
            game.Mouse.MouseDown += MouseOnMouseDown;
            game.Mouse.MouseUp += MouseOnMouseUp;
            game.Mouse.MouseDoubleClick += MouseOnDoubleClick;
            CommandBuffer = game.Commands;
        }

        public string GetClipboardText()
        {
            if (game != null) return game.GetClipboardText();
            return "";
        }

        public void SetClipboardText(string text) =>
            game?.SetClipboardText(text);

        private void MouseOnDoubleClick(MouseEventArgs e)
        {
            if (game.Debug.CaptureMouse) return;
            if ((e.Buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                OnMouseDoubleClick();
            }
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

        public Vector2 PixelsToPoints(Vector2 pixels) => pixels * (480f / ViewportHeight);

        public float PixelsToPoints(float pixels) => pixels * (480 / ViewportHeight);

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
            Update(baseWidget, game.TotalTime,
                game.Mouse.X, game.Mouse.Y, game.Mouse.IsButtonDown(MouseButtons.Left));
        }

        public bool HasModal => modals.Count > 0;
        public bool MouseWanted(int mouseX, int mouseY)
        {
            var inputRatio = 480 / ViewportHeight;
            if (modals.Count > 0) return true;
            if (!Visible) return false;
            return baseWidget?.MouseWanted(this, GetRectangle(), mouseX * inputRatio, mouseY * inputRatio) ?? false;
        }

        public bool WantsEscape()
        {
            if (modals.Count > 0) return true;
            if (!Visible) return false;
            if (textFocusWidget != null) return true;
            return baseWidget?.WantsEscape() ?? false;
        }

        public void OnFocus()
        {
            baseWidget.UnFocus();
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

        public void PlayVoiceLine(string voice, string line)
        {
            Data.Sounds?.PlayVoiceLine(voice, FLHash.CreateID(line), null);
        }

        class ModalState
        {
            public UiWidget Widget;
            public int Handle;
        }

        public bool Visible = true;
        UiWidget GetActive()
        {
            if (modals.Count > 0) return modals[modals.Count - 1].Widget;
            if(!Visible) return null;
            if (baseWidget == null) return null;
            return baseWidget;
        }

        public void ChatboxEvent() => Event("Chatbox");
        public void Event(string ev)
        {
            lua.CallEvent(ev);
        }
        public void Event(string ev, params object[] p)
        {
            lua.CallEvent(ev, p);
        }

        public void OnEscapePressed()
        {
            GetActive()?.OnEscapePressed();
        }

        public void OnMouseDown() => GetActive()?.OnMouseDown(this, GetRectangle());
        public void OnMouseUp() => GetActive()?.OnMouseUp(this, GetRectangle());
        public void OnMouseClick() => GetActive()?.OnMouseClick(this, GetRectangle());

        public void OnMouseDoubleClick() => GetActive()?.OnMouseDoubleClick(this, GetRectangle());

        public void OnMouseWheel(float delta) => GetActive()?.OnMouseWheel(this, GetRectangle(), delta);

        private UiWidget textFocusWidget = null;

        internal void SetTextFocus(UiWidget widget) => textFocusWidget = widget;

        public bool KeyboardGrabbed => textFocusWidget != null;

        public void OnKeyDown(Keys key, bool control) => textFocusWidget?.OnKeyDown(this, key, control);

        public void OnTextEntry(string text) => textFocusWidget?.OnTextInput(text);

        public double DeltaTime;
        public void RenderWidget(double delta)
        {
            DeltaTime = delta;
            if (baseWidget == null)
            {
                textFocusWidget = null;
                return;
            }
            if (game != null && game.Mouse.Wheel != 0)
            {
                OnMouseWheel(game.Mouse.Wheel);
            }
            textFocusWidget = null;
            RenderContext.DepthEnabled = false;
            var aspect = ViewportWidth / ViewportHeight;
            var desktopRect = new RectangleF(0, 0, 480 * aspect, 480);
            if(Visible)
                baseWidget.Render(this, desktopRect);
            foreach(var widget in modals)
                widget.Widget.Render(this, desktopRect);
            RenderContext.DepthEnabled = true;
        }
    }
}
