using System;
using System.Collections.Generic;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[UiLoadable]
[WattleScriptUserData]
public class ChatHistory : UiWidget
{
    public ChatSource Chat = new ChatSource();

    private BuiltRichText builtText;
    private float builtMultiplier = 0;
    private int builtVersion;
    private float lastHeight = 0;

    private Scrollbar scrollbar = new Scrollbar() {Smooth = true};
    private bool scrollbarVisible = false;

    public override void ApplyStylesheet(Stylesheet sheet)
    {
        scrollbar.ApplyStyle(sheet);
    }

    RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
    {
        var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
        var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
        return myRectangle;
    }

    public override void Render(UiContext context, RectangleF parentRectangle)
    {
        var myRectangle = GetMyRectangle(context, parentRectangle);
        Background?.Draw(context, myRectangle);
        myRectangle.Width -= scrollbar.Style.Width;
        var textMultiplier = (context.ViewportHeight / 480) * 0.5f;
        var displayRect = context.PointsToPixels(myRectangle);

        if (Math.Abs(builtMultiplier - textMultiplier) > 0.01f ||
            builtText == null ||
            Chat.Version != builtVersion)
        {
            builtText?.Dispose();
            var nodes = new List<RichTextNode>();
            for (int i = 0; i < Chat.Messages.Count; i++) {
                nodes.AddRange(Chat.Messages[i].Nodes);
                if(i < (Chat.Messages.Count - 1)) nodes.Add(new RichTextParagraphNode());
            }
            builtText = context.RenderContext.Renderer2D.CreateRichTextEngine().BuildText(nodes,
                displayRect.Width, textMultiplier);
            builtMultiplier = textMultiplier;
        }
        builtText.Recalculate(displayRect.Width);

        var h = builtText.Height;
        if (lastHeight != h)
        {
            lastHeight = h;
            if ((int) h > displayRect.Height + 2)
            {
                if (!scrollbarVisible || scrollbar.ScrollOffset == 1) {
                    scrollbar.ScrollOffset = 1;
                }
                scrollbar.ThumbSize = displayRect.Height / h;
                const float TICK_MAGIC = 0.2627986f;
                scrollbar.Tick = 0.01f * (scrollbar.ThumbSize / TICK_MAGIC);
                scrollbarVisible = true;
            }
            else
            {
                scrollbarVisible = false;
            }
        }
        if(scrollbarVisible)
            scrollbar.Render(context, new RectangleF(myRectangle.X + myRectangle.Width, myRectangle.Y, scrollbar.Style.Width, myRectangle.Height));
        context.RenderContext.ScissorEnabled = true;
        context.RenderContext.ScissorRectangle = displayRect;
        int y = displayRect.Y;
        if (scrollbarVisible) {
            y -= (int) (scrollbar.ScrollOffset * (builtText.Height - displayRect.Height));
        }
        context.RenderContext.Renderer2D.CreateRichTextEngine().RenderText(builtText, displayRect.X, y);
        context.RenderContext.ScissorEnabled = false;
        Border?.Draw(context, myRectangle);
    }

    public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
    {
        var myRectangle = GetMyRectangle(context, parentRectangle);
        if(scrollbarVisible)
            scrollbar.OnMouseDown(context, myRectangle);
    }

    public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
    {
        var myRectangle = GetMyRectangle(context, parentRectangle);
        if (scrollbarVisible)
            scrollbar.OnMouseUp(context, myRectangle);
    }

    public override void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta)
    {
        var myRectangle = GetMyRectangle(context, parentRectangle);
        if (scrollbarVisible &&
            myRectangle.Contains(context.MouseX, context.MouseY))
            scrollbar.OnMouseWheel(delta);
    }
}
