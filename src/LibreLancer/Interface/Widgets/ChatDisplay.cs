// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ChatDisplay: UiWidget
    {
        public ChatSource Chat = new ChatSource();

        private BuiltRichText builtText;
        private float builtMultiplier = 0;
        private ChatSource.DisplayMessage[] buildMessages = Array.Empty<ChatSource.DisplayMessage>();

        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            return myRectangle;
        }

        ChatSource.DisplayMessage[] GetMessageIds()
        {
            lock (Chat.Messages)
            {
                List<ChatSource.DisplayMessage> ids = new();
                for (int i = Chat.Messages.Count - 1; i >= 0 && (i >= Chat.Messages.Count - 16); i--)
                {
                    var msg = Chat.Messages[i];
                    if (msg.TimeAlive <= 0) continue;
                    ids.Add(msg);
                }
                return ids.ToArray();
            }
        }

        bool IdChanged(ChatSource.DisplayMessage[] src)
        {
            if (src.Length != buildMessages.Length) return true;
            for (int i = 0; i < src.Length; i++) {
                if (src[i] != buildMessages[i]) return true;
            }
            return false;
        }

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            var rect = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, rect);
            var ids = GetMessageIds();
            for (int i = Chat.Messages.Count - 1; i >= 0 && (i >= Chat.Messages.Count - 16); i--) {
                Chat.Messages[i].TimeAlive -= (float)context.DeltaTime;
            }

            if (ids.Length > 0)
            {
                var textMultiplier = (context.ViewportHeight / 480) * 0.5f;
                var displayRect = context.PointsToPixels(rect);

                if (Math.Abs(builtMultiplier - textMultiplier) > 0.01f ||
                    builtText == null ||
                    IdChanged(ids))
                {
                    builtText?.Dispose();
                    var nodes = new List<RichTextNode>();
                    for (int i = ids.Length - 1; i >= 0; i--) {
                        nodes.AddRange(ids[i].Nodes);
                        if(i > 0) nodes.Add(new RichTextParagraphNode());
                    }
                    builtText = context.RenderContext.Renderer2D.CreateRichTextEngine().BuildText(nodes,
                        displayRect.Width, textMultiplier);
                    buildMessages = ids;
                    builtMultiplier = textMultiplier;
                }

                builtText.Recalculate(displayRect.Width);
                context.RenderContext.Renderer2D.CreateRichTextEngine().RenderText(builtText,
                    displayRect.X, (int)(displayRect.Y + displayRect.Height - builtText.Height));
            }

            Border?.Draw(context, rect);
        }
    }
}
