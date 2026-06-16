// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using LibreLancer.Render;

namespace LibreLancer.Interface
{
    public class UiContext
    {
        // State
        public float ViewportWidth;
        public float ViewportHeight;
        public float MouseX;
        public float MouseY;
        public bool MouseLeftDown;
        public double GlobalTime;

        public float ScreenWidth => 480 * (ViewportWidth / ViewportHeight);

        // Rendering
        public RenderContext RenderContext = null!;

        public LineRenderer Lines = null!;

        // Data
        public UiData Data;

        public CommandBuffer CommandBuffer = null!;

        // Ui
        private object _gameApi = null!;

        // TODO: Properly reload RigidModels on meshes cleared
        public int MeshDisposeVersion = 0;

        public object GameApi
        {
            get { return _gameApi; }
            set
            {
                _gameApi = value;
                lua?.SetGameApi(_gameApi);
            }
        }

        private LuaContext lua = null!;

        // State
        private bool mode2d = false;
        private FreelancerGame? game;

        public VertexBuffer? NavmapBuffer;

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
            if (game != null)
            {
                return game.GetClipboardText()!;
            }

            return "";
        }

        public void SetClipboardText(string text) =>
            game?.SetClipboardText(text);

        private void MouseOnDoubleClick(MouseEventArgs e)
        {
            if (game!.Debug.CaptureMouse)
            {
                return;
            }

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

        public void OpenScene(string scene, params object[] args)
        {
            lua.OpenScene(scene, args);
        }


        private void MouseOnMouseUp(MouseEventArgs e)
        {
            if (game!.Debug.CaptureMouse)
            {
                return;
            }

            if ((e.Buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                OnMouseClick();
                OnMouseUp();
            }
        }

        private void MouseOnMouseDown(MouseEventArgs e)
        {
            if (game!.Debug.CaptureMouse)
            {
                return;
            }

            if ((e.Buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                OnMouseDown();
            }
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

        // Points->Pixels

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
            return new Rectangle((int) (points.X * ratio), (int) (points.Y * ratio), (int) (points.Width * ratio),
                (int) (points.Height * ratio));
        }

        // Pixels->Points

        public Vector2 PixelsToPoints(Point pixels) => PixelsToPoints(new Vector2(pixels.X, pixels.Y));

        public Vector2 PixelsToPoints(Vector2 pixels) => pixels * (480f / ViewportHeight);

        public float PixelsToPoints(float pixels) => pixels * (480 / ViewportHeight);


        public float TextSize(float inputPoints)
        {
            var ratio = ViewportHeight / 480;
            var pixels = inputPoints * ratio;
            return (int) Math.Floor(pixels);
        }

        public void Update(double globalTime, double deltaTime, int mouseX, int mouseY, bool leftDown)
        {
            GlobalTime = globalTime;
            var inputRatio = 480 / ViewportHeight;
            MouseX = mouseX * inputRatio;
            MouseY = mouseY * inputRatio;
            MouseLeftDown = leftDown;
            lua?.DoTimers(globalTime);
            lua?.CallEvent("Update", globalTime);
            UpdateWidgets(deltaTime);
        }

        void UpdateWidgets(double delta)
        {
            var layout = new Layout(GetRectangle());
            if (Visible)
            {
                baseWidget?.Update(this, delta);
                baseWidget?.OnLayout(this, layout, delta);
            }
            foreach (var modal in modals)
            {
                modal.Widget.Update(this, delta);
                modal.Widget.OnLayout(this, layout, delta);
            }
        }

        public void Update(FreelancerGame game, double deltaTime)
        {
            ViewportWidth = game.Width;
            ViewportHeight = game.Height;
            Update(game.TotalTime, deltaTime, game.Mouse.X, game.Mouse.Y, game.Mouse.IsButtonDown(MouseButtons.Left));
        }

        public bool HasModal => modals.Count > 0;

        public bool MouseWanted(int mouseX, int mouseY)
        {
            var inputRatio = 480 / ViewportHeight;

            if (modals.Count > 0)
            {
                return true;
            }

            if (!Visible)
            {
                return false;
            }

            return baseWidget?.MouseWanted(this, mouseX * inputRatio, mouseY * inputRatio) ?? false;
        }

        public bool WantsEscape()
        {
            if (modals.Count > 0)
            {
                return true;
            }

            if (!Visible)
            {
                return false;
            }

            if (textFocusWidget != null)
            {
                return true;
            }

            return baseWidget?.WantsEscape() ?? false;
        }

        public void OnFocus()
        {
            baseWidget?.UnFocus();
        }

        private RectangleF GetRectangle() => new(0, 0, 480 * (ViewportWidth / ViewportHeight), 480);

        private UiWidget? baseWidget;
        private List<ModalState> modals = [];

        public void SetWidget(UiWidget widget)
        {
            foreach (var m in modals)
                m.Widget.Dispose();
            modals = [];
            baseWidget = widget;
        }

        private int _h = 0;

        public int OpenModal(UiWidget widget)
        {
            var handle = _h++;
            modals.Add(new ModalState(widget, handle));
            return handle;
        }

        public void SwapModal(UiWidget widget, int handle)
        {
            foreach (var modal in modals)
            {
                if (modal.Handle == handle)
                {
                    modal.Widget = widget;
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

        public void LoadSound(string sound)
        {
            Data.Sounds?.LoadSound(sound);
        }

        public void PlayVoiceLine(string voice, string line)
        {
            Data.Sounds?.PlayVoiceLine(voice, line, null);
        }

        private class ModalState(UiWidget widget, int handle)
        {
            public UiWidget Widget = widget;
            public int Handle = handle;
        }

        public bool Visible = true;

        private UiWidget? GetActive()
        {
            if (modals.Count > 0)
            {
                return modals[modals.Count - 1].Widget;
            }

            return !Visible ? null : baseWidget;
        }

        public void ChatboxEvent() => Event("Chatbox");

        public void Event(string ev)
        {
            lua.CallEvent(ev);
        }

        public void Event(string ev, params object?[] p)
        {
            lua.CallEvent(ev, p);
        }

        public void OnEscapePressed()
        {
            GetActive()?.OnEscapePressed();
        }

        public void OnMouseDown() => GetActive()?.OnMouseDown(this);
        public void OnMouseUp() => GetActive()?.OnMouseUp(this);
        public void OnMouseClick() => GetActive()?.OnMouseClick(this);

        public void OnMouseDoubleClick() => GetActive()?.OnMouseDoubleClick(this);

        public void OnMouseWheel(float delta) => GetActive()?.OnMouseWheel(this, delta);

        private UiWidget? textFocusWidget = null;

        internal void SetTextFocus(UiWidget widget) => textFocusWidget = widget;

        public bool KeyboardGrabbed => textFocusWidget != null;

        public void OnKeyDown(Keys key, bool control) => textFocusWidget?.OnKeyDown(this, key, control);

        public void OnTextEntry(string text) => textFocusWidget?.OnTextInput(text);

        private string? requestedRollover = null;
        private CachedRenderString? rolloverCache;

        private string? requestedTooltip = null;
        private RectangleF tooltipParent;
        private CachedRenderString? tooltipCache;

        public void SetRollover(int itemStrid)
        {
            if (Data.RolloverMap.TryGetValue(itemStrid, out var rollStrid) &&
                Data.Infocards != null &&
                Data.Infocards.HasStringResource(rollStrid))
            {
                requestedRollover = Data.Infocards.GetStringResource(rollStrid);
            }
        }

        public void SetTooltip(string text, RectangleF controlRectangle)
        {
            requestedTooltip = text;
            tooltipParent = controlRectangle;
        }

        public void RenderWidget(double delta)
        {
            requestedRollover = null;
            requestedTooltip = null;

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
            var dlist = RenderContext.Renderer2D.CreateDrawList();
            var aspect = ViewportWidth / ViewportHeight;

            if (Visible)
            {
                baseWidget.Render(this, delta, dlist);
            }

            foreach (var widget in modals)
                widget.Widget.Render(this, delta, dlist);

            if (!string.IsNullOrWhiteSpace(requestedRollover))
            {
                var style = Data.Stylesheet?.Styles.DefaultStyle<RolloverStyle>();

                var maxWidth = PointsToPixels(225);

                var fnt = Data.GetFont(style?.Font ?? "Arial");
                var sz = TextSize(Data.GetFontSize(style?.Font ?? "Arial"));

                var col = style?.TextColor?.GetColor(GlobalTime) ?? Color4.White;
                var shadow = style?.TextShadow?.GetColor(GlobalTime);

                var measuredText = RenderContext.Renderer2D.MeasureStringCached(
                    ref rolloverCache, fnt, sz, requestedRollover, false,
                    shadow != null, TextAlignment.Left, maxWidth);
                var rectSize = PixelsToPoints(measuredText);

                var ttRect = new RectangleF(0, 0, rectSize.X + 4, rectSize.Y + 2);
                style?.Background?.Draw(this, dlist, ttRect);

                var offsetX = PointsToPixels(2);
                var offsetY = PointsToPixels(1);

                dlist.DrawStringCached(ref rolloverCache, fnt, sz, requestedRollover,
                    offsetX, offsetY, col, false,
                    shadow != null ? new(shadow.Value) : default,
                    TextAlignment.Left, maxWidth);

                style?.Border?.Draw(this, dlist, ttRect);
            }

            if (!string.IsNullOrWhiteSpace(requestedTooltip))
            {
                var style = Data.Stylesheet?.Styles.DefaultStyle<TooltipStyle>();

                var maxWidth = PointsToPixels(225);

                var fnt = Data.GetFont(style?.Font ?? "Arial");
                var sz = TextSize(Data.GetFontSize(style?.Font ?? "Arial"));

                var col = style?.TextColor?.GetColor(GlobalTime) ?? Color4.White;
                var shadow = style?.TextShadow?.GetColor(GlobalTime);

                var measuredText = RenderContext.Renderer2D.MeasureStringCached(
                    ref tooltipCache, fnt, sz, requestedTooltip, false,
                    shadow != null, TextAlignment.Left, maxWidth);
                var rectSize = PixelsToPoints(measuredText);

                var rectOffset = style?.OffsetY ?? 0;

                var ttRect = new RectangleF(tooltipParent.X, tooltipParent.Y + tooltipParent.Height + rectOffset,
                    rectSize.X + 4, rectSize.Y + 2);
                style?.Background?.Draw(this, dlist, ttRect);

                var scrTtRect = PointsToPixels(ttRect);

                var posX = scrTtRect.X + PointsToPixels(2);
                var posY = scrTtRect.Y + PointsToPixels(1);

                dlist.DrawStringCached(ref tooltipCache, fnt, sz, requestedTooltip,
                    posX, posY, col, false,
                    shadow != null ? new(shadow.Value) : default,
                    TextAlignment.Left, maxWidth);

                style?.Border?.Draw(this, dlist, ttRect);
            }

            dlist.Render();
        }

        public void Dispose()
        {
            NavmapBuffer?.Dispose();
        }
    }
}
