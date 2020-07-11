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
        public TimeSpan GlobalTime;
        //Rendering
        public RenderState RenderState;
        public Renderer2D Renderer2D;
        public MatrixCamera MatrixCam = new MatrixCamera(Matrix4x4.Identity);
        //Data
        public UiData Data;
        //Ui
        public object GameApi;

        //State
        private bool mode2d = false;
        private FreelancerGame game;
        public UiContext(UiData data)
        {
            Data = data;
        }
        
        public UiContext(FreelancerGame game, string file)
        {
            Renderer2D = game.Renderer2D;
            RenderState = game.RenderState;
            Data = new UiData(game);
            this.game = game;
            game.Mouse.MouseDown += MouseOnMouseDown;
            game.Mouse.MouseUp += MouseOnMouseUp;
            var w = Data.LoadXml(file);
            w.ApplyStylesheet(Data.Stylesheet);
            SetWidget(w);
        }

        public void Start()
        {
            baseWidget.EnableScripting(this, null);
        }
        private void MouseOnMouseUp(MouseEventArgs e)
        {
            if ((e.Buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                OnMouseClick();
                OnMouseUp();
            }
        }

        private void MouseOnMouseDown(MouseEventArgs e)
        {
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

        public int PointsToPixels(float points)
        {
            var ratio = ViewportHeight / 480;
            var i = (int) (points * ratio);
            return i <= 0 ? 1 : i;
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
        
        public void Update(UiWidget widget, TimeSpan globalTime, int mouseX, int mouseY, bool leftDown)
        {
            GlobalTime = globalTime;
            var inputRatio = 480 / ViewportHeight;
            MouseX = mouseX * inputRatio;
            MouseY = mouseY * inputRatio;
            MouseLeftDown = leftDown;
        }

        public void Update(FreelancerGame game)
        {
            ViewportWidth = game.Width;
            ViewportHeight = game.Height;
            if(Visible)
            Update(baseWidget, TimeSpan.FromSeconds(game.TotalTime),
                game.Mouse.X, game.Mouse.Y, game.Mouse.IsButtonDown(MouseButtons.Left));
        }

        public void OnFocus()
        {
            baseWidget.UnFocus();
        }

        public void Dispose()
        {
            if (game != null)
            {
                baseWidget.Dispose();
                game.Mouse.MouseUp -= MouseOnMouseUp;
                game.Mouse.MouseDown -= MouseOnMouseDown;
            }
        }
        RectangleF GetRectangle() => new RectangleF(0,0, 480 * (ViewportWidth / ViewportHeight), 480);

        private UiWidget baseWidget;
        Stack<ModalState> modals = new Stack<ModalState>();
        public void SetWidget(UiWidget widget)
        {
            foreach (var m in modals)
                m.Widget.Dispose();
            modals = new Stack<ModalState>();
            baseWidget = widget;
        }
        public void OpenModal(string name, string modalData, Action<string> onClose)
        {
            var item = Data.LoadXml(name);
            item.EnableScripting(this, modalData);
            modals.Push(new ModalState() {
                Widget = item, OnClose = onClose
            });
        }

        public void CloseModal(string data)
        {
            if (modals.Count > 0)
            {
                modals.Peek().OnClose?.Invoke(data);
                modals.Pop();
            }
        }

        public void PlaySound(string sound)
        {
            Data.Sounds?.PlaySound(sound);
        }
        
        class ModalState
        {
            public UiWidget Widget;
            public Action<string> OnClose;
        }

        public bool Visible = true;
        UiWidget GetActive()
        {
            if(!Visible) return null;
            if (baseWidget == null) return null;
            if (modals.Count > 0) return modals.Peek().Widget;
            return baseWidget;
        }

        public void ChatboxEvent() =>   GetActive()?.ScriptedEvent("Chatbox");
        public void Event(string ev)
        {
            GetActive()?.ScriptedEvent(ev);
        }
        public void OnMouseDown() => GetActive()?.OnMouseDown(this, GetRectangle());
        public void OnMouseUp() => GetActive()?.OnMouseUp(this, GetRectangle());
        public void OnMouseClick() => GetActive()?.OnMouseClick(this, GetRectangle());

        private UiWidget textFocusWidget = null;

        internal void SetTextFocus(UiWidget widget) => textFocusWidget = widget;

        public bool KeyboardGrabbed => textFocusWidget != null;

        public void OnKeyDown(Keys key) => textFocusWidget?.OnKeyDown(key);

        public void OnTextEntry(string text) => textFocusWidget?.OnTextInput(text);
        public void RenderWidget()
        {
            if (baseWidget == null || !Visible)
            {
                textFocusWidget = null;
                return;
            }
            textFocusWidget = null;
            RenderState.DepthEnabled = false;
            mode2d = false;
            var aspect = ViewportWidth / ViewportHeight;
            var desktopRect = new RectangleF(0, 0, 480 * aspect, 480);
            baseWidget.Render(this, desktopRect);
            foreach(var widget in modals.Reverse())
                widget.Widget.Render(this, desktopRect);
            if (mode2d)
                Renderer2D.Finish();
            RenderState.DepthEnabled = true;
        }
    }
}