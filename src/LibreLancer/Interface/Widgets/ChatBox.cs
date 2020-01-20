// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;

namespace LibreLancer.Interface
{
    [UiLoadable]
    public class ChatBox : UiWidget
    {
        public UiRenderable Background { get; set; }
        public event Action<string> TextEntered;
        public string CurrentEntry = "Console->";
        public string CurrentText = "";
        public int MaxChars = 100;

        public ChatBox() : base()
        {
            Visible = false;
        }
        
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            context.SetTextFocus(this);
        }

        public override void OnKeyDown(Keys key)
        {
            if (key == Keys.Enter)
            {
                if(!string.IsNullOrWhiteSpace(CurrentText)) TextEntered?.Invoke(CurrentText);
                CurrentText = "";
                Visible = false;
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