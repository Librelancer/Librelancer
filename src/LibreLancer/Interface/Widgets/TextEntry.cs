// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
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

        public string CurrentText = "";
        public int MaxChars = 100;
        public float FontSize { get; set; } = 10f;
        public string Font { get; set; }
        public InterfaceColor TextColor { get; set; }
        public InterfaceColor TextShadow { get; set; }
        public UiRenderable FocusedBorder { get; set; }

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
            DrawText(context, ref renderCache, myRect, FontSize, Font, TextColor, TextShadow, HorizontalAlignment.Left,
                VerticalAlignment.Center,
                true, (hasFocus && cursorVisible) ? CurrentText + "|" : CurrentText);
        }
        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            return myRect;
        }
        public override void OnKeyDown(Keys key)
        {
            if (key == Keys.Enter)
            {
                if(!string.IsNullOrWhiteSpace(CurrentText)) TextEntered?.Invoke(CurrentText);
            }

            if (key == Keys.Backspace)
            {
                if(CurrentText.Length > 0)
                    CurrentText = CurrentText.Substring(0, CurrentText.Length - 1);
            }
        }
        
        public override void OnTextInput(string text)
        {
            if (CurrentText.Length + text.Length > MaxChars)
                return;
            CurrentText += text;
        }
        
    }
}