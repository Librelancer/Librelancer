// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using LibreLancer.Graphics.Text;
using SharpDX.MediaFoundation;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class TextEntry : UiWidget
    {
        event Action<string> TextEntered;

        public void OnTextEntered(WattleScript.Interpreter.Closure handler)
        {
            TextEntered += (s) =>
            {
                handler.Call(s);
            };
        }

        private TextEditBase editBase = new TextEditBase(false) {Focused = false, Wrap = false};

        public string CurrentText
        {
            get => editBase.Text;
            set => editBase.Text = value;
        }
        public int MaxChars = 100;
        public float FontSize { get; set; } = 10f;
        public string Font { get; set; }
        public InterfaceColor TextColor { get; set; }
        public InterfaceColor TextShadow { get; set; }
        public UiRenderable FocusedBorder { get; set; }

        public bool Password
        {
            get => editBase.Mask;
            set => editBase.Mask = value;
        }

        private bool doSetFocus = false;
        private bool hasFocus = false;
        private double lastChange = 0.0;
        private double blinkDuration = 0.4;
        private bool cursorVisible = false;
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (context.GlobalTime - lastChange > blinkDuration) {
                lastChange = context.GlobalTime;
                cursorVisible = !cursorVisible;
            }
            if (!Visible) return;
            if (doSetFocus) {
                context.OnFocus();
                doSetFocus = false;
                hasFocus = true;
            }
            if(hasFocus)
                context.SetTextFocus(this);
            var rect = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, rect);
            DrawText(context, rect);
            (hasFocus ? FocusedBorder ?? Border : Border)?.Draw(context, rect);
        }

        public override void OnMouseClick(UiContext context, RectangleF parentRectangle)
        {
            if(!Visible) return;
            var myRect = GetMyRectangle(context, parentRectangle);
            if (myRect.Contains(context.MouseX, context.MouseY))
                SetFocus();
            else
                UnFocus();
        }

        public override void UnFocus()
        {
            doSetFocus = false;
            hasFocus = false;
        }

        public void SetFocus()
        {
            doSetFocus = true;
        }

        private CachedRenderString renderCache;

        void DrawText(UiContext context, RectangleF myRect)
        {
            //Padding
            myRect.X += 2;
            myRect.Width -= 4;
            //Draw
            var size = context.TextSize(FontSize <= 0 ? 10 : FontSize);
            editBase.FontSize = size;
            if (TextShadow != null)
                editBase.FontShadow = new OptionalColor(TextShadow.GetColor(context.GlobalTime));
            else
                editBase.FontShadow = new OptionalColor();
            editBase.FontColor = (TextColor ?? InterfaceColor.White).GetColor(context.GlobalTime);
            editBase.FontName = context.Data.GetFont(Font);
            var px = context.PointsToPixels(myRect);
            //Vertical alignment hacky
            px.Y += (int)((px.Height / 2f) -
                    (context.RenderContext.Renderer2D.LineHeight(editBase.FontName, editBase.FontSize) / 2f));
            editBase.SetRectangle(px);
            editBase.Focused = hasFocus;
            editBase.Draw(context.RenderContext, context.GlobalTime);
        }
        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            return myRect;
        }

        public override void OnKeyDown(UiContext context, Keys key, bool control)
        {
            switch (key)
            {
                case Keys.A when control:
                    editBase.SelectAll();
                    break;
                case Keys.V when control:
                    OnTextInput(context.GetClipboardText());
                    break;
                case Keys.C when control && editBase.Selected:
                    context.SetClipboardText(editBase.Text);
                    break;
                case Keys.Enter:
                    if(!string.IsNullOrWhiteSpace(CurrentText)) TextEntered?.Invoke(CurrentText);
                    editBase.Unselect();
                    break;
                case Keys.Left:
                    editBase.CaretLeft();
                    break;
                case Keys.Right:
                    editBase.CaretRight();
                    break;
                case Keys.Escape:
                    editBase.Unselect();
                    break;
                case Keys.Delete:
                    editBase.Delete();
                    break;
                case Keys.Backspace:
                    editBase.Backspace();
                    break;
            }
        }

        public bool NotEmpty => !string.IsNullOrWhiteSpace(CurrentText);

        public override void OnTextInput(string text)
        {
            if (CurrentText.Length + text.Length > MaxChars)
                return;
            editBase.TextEntered(text);
        }

    }
}
