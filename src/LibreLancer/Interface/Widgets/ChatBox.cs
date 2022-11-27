// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using LibreLancer.Net;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ChatBox : UiWidget
    {
        event Action<ChatCategory, string> TextEntered;
        public void OnTextEntered(WattleScript.Interpreter.Closure handler)
        {
            TextEntered += (c,s) =>
            {
                handler.Call(c, s);
            };
        }

        public ChatCategory Category = ChatCategory.System;

        public string CurrentEntry
        {
            get
            {
                switch (Category)
                {
                    case ChatCategory.Console: return "Console->";
                    case ChatCategory.Local: return "Local->";
                    case ChatCategory.System: return "System->";
                    default: return ">";
                }
            }
        }
        
        public string CurrentText = "";
        public int MaxChars = 100;
        public float FontSize { get; set; } = 12f;
        
        public ChatBox() : base()
        {
            Visible = false;
        }
        
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            context.SetTextFocus(this);
            var rect = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, rect);
            DrawText(context, rect);
            Border?.Draw(context, rect);
        }

        void DrawText(UiContext context, RectangleF myRect)
        {
            var sizeF = context.TextSize(FontSize);
            var node0 = new RichTextTextNode()
            {
                Contents = CurrentEntry, FontName = "Arial", FontSize = sizeF, Color = Category.GetColor(), Shadow = new TextShadow(Color4.Black)
            };
            var node1 = new RichTextTextNode()
            {
                Contents = CurrentText, FontName = "Arial", FontSize = sizeF, Shadow = new TextShadow(Color4.Black)
            };
            var rtf = context.RenderContext.Renderer2D.CreateRichTextEngine();
            var rect = context.PointsToPixels(myRect);
            var built = rtf.BuildText(new[] {node0, node1}, (int) rect.Width - 4, 1f);
            context.RenderContext.ScissorEnabled = true;
            context.RenderContext.ScissorRectangle = rect;
            rtf.RenderText(built, (int)rect.X + 2, (int)rect.Y + 2);
            context.RenderContext.ScissorEnabled = false;
            built.Dispose();
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
            if (control && key == Keys.V)
            {
                OnTextInput(context.GetClipboardText());
            }
            if (key == Keys.Enter)
            {
                if(!string.IsNullOrWhiteSpace(CurrentText)) TextEntered?.Invoke(Category, CurrentText);
                CurrentText = "";
                Visible = false;
            }
            if (key == Keys.Up)
            {
                Category--;
                if (Category < 0) Category = ChatCategory.MAX - 1;
            }
            if (key == Keys.Down)
            {
                Category++;
                if (Category >= ChatCategory.MAX) Category = 0;
            }
            if (key == Keys.Escape)
            {
                CurrentText = "";
                Visible = false;
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