// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [MoonSharpUserData]
    public class ChatDisplay: UiWidget
    {
        public ChatSource Chat = new ChatSource();
        
        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            return myRectangle;
        }
        
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            var rect = GetMyRectangle(context, parentRectangle);
            context.Mode2D();
            Background?.Draw(context, rect);
            float y = rect.Y + rect.Height;
            float dt = (float) context.DeltaTime;
            lock (Chat.Messages)
            {
                for (int i = Chat.Messages.Count - 1; i >= 0 && (i >= Chat.Messages.Count - 16); i--)
                {
                    var msg = Chat.Messages[i];
                    if (msg.TimeAlive <= 0) continue;
                    msg.TimeAlive -= dt;
                    var sz = context.Renderer2D.MeasureStringCached(ref msg.Cache, msg.Font, context.TextSize(msg.Size),
                        msg.Text);
                    float h = context.PixelsToPoints(sz.Y);
                    y -= h;
                    DrawText(context, ref msg.Cache, new RectangleF(rect.X, y, rect.Width, h), msg.Size, msg.Font,
                        msg.Color, InterfaceColor.Black, HorizontalAlignment.Left, VerticalAlignment.Default,
                        false, msg.Text);
                    y -= 2;
                }
            }
            Border?.Draw(context, rect);
        }
    }
}