// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using LibreLancer.Graphics.Text;
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
            TextEntered += (c, s) => { handler.Call(c, s); };
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

        public string CurrentText
        {
            get => editBase.Text;
            set => editBase.Text = value;
        }

        public int MaxChars = 160;
        public float FontSize { get; set; } = 12f;

        private TextEditBase editBase = new TextEditBase(true)
        {
            Focused = true,
            Wrap = true
        };

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
            editBase.LeadingNode  = new RichTextTextNode()
            {
                Contents = CurrentEntry, FontName = "Arial", FontSize = sizeF, Color = Category.GetColor(), Shadow = new OptionalColor(Color4.Black)
            };
            var rect = context.PointsToPixels(myRect);
            editBase.FontSize = sizeF;
            editBase.FontShadow = new OptionalColor(Color4.Black);
            editBase.SetRectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
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
                    if(!string.IsNullOrWhiteSpace(CurrentText)) TextEntered?.Invoke(Category, CurrentText);
                    editBase.Unselect();
                    CurrentText = "";
                    Visible = false;
                    break;
                case Keys.Up:
                    Category--;
                    if (Category < 0) Category = ChatCategory.MAX - 1;
                    break;
                case Keys.Down:
                    Category++;
                    if (Category >= ChatCategory.MAX) Category = 0;
                    break;
                case Keys.Left:
                    editBase.CaretLeft();
                    break;
                case Keys.Right:
                    editBase.CaretRight();
                    break;
                case Keys.Escape:
                    editBase.Unselect();
                    CurrentText = "";
                    Visible = false;
                    break;
                case Keys.Delete:
                    editBase.Delete();
                    break;
                case Keys.Backspace:
                    editBase.Backspace();
                    break;
            }
        }

        public override void OnTextInput(string text)
        {
            if (CurrentText.Length + text.Length > MaxChars)
                return;
            editBase.TextEntered(text);
        }

    }
}
