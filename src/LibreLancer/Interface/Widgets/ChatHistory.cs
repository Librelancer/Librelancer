using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace LibreLancer.Interface;

[UiLoadable]
[WattleScriptUserData]
public class ChatHistory : UiWidget
{
    public ChatSource Chat = new();

    private BuiltRichText? builtText;
    private float builtMultiplier = 0;
    private int builtVersion;
    private float lastHeight = 0;
    public Scrollbar Scrollbar { get; set; } = new();


    public override void OnLayout(UiContext context, Layout layout, double delta)
    {
        base.OnLayout(context, layout, delta);
        Scrollbar.OnLayout(context, new Layout(ClientRectangle), delta);
    }


    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public override void Render(UiContext context, double delta, DrawList2D drawList)
    {
        var myRectangle = ClientRectangle;
        Background?.Draw(context, drawList, myRectangle);
        myRectangle.Width -= Scrollbar.ClientRectangle.Width;
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
            builtText = context.RenderContext.Renderer2D.RichText.BuildText(nodes,
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
                if (!Scrollbar.Visible || Scrollbar.ScrollOffset == 1) {
                    Scrollbar.ScrollOffset = 1;
                }
                Scrollbar.ThumbSize = displayRect.Height / h;
                const float TICK_MAGIC = 0.2627986f;
                Scrollbar.Tick = 0.01f * (Scrollbar.ThumbSize / TICK_MAGIC);
                Scrollbar.Visible = true;
            }
            else
            {
                Scrollbar.Visible = false;
            }
        }

        if (Scrollbar.Visible)
            Scrollbar.Render(context, delta, drawList);
        if (drawList.PushClip(displayRect))
        {
            int y = displayRect.Y;
            if (Scrollbar.Visible) {
                y -= (int) (Scrollbar.ScrollOffset * (builtText.Height - displayRect.Height));
            }
            context.RenderContext.Renderer2D.RichText.RenderText(drawList, builtText, displayRect.X, y);
            drawList.PopClip();
        }
        Border?.Draw(context, drawList, myRectangle);
    }

    public override void OnMouseDown(UiContext context)
    {
        Scrollbar.OnMouseDown(context);
    }

    public override void OnMouseUp(UiContext context)
    {
        Scrollbar.OnMouseUp(context);
    }

    public override void OnMouseWheel(UiContext context, float delta)
    {
        if (ClientRectangle.Contains(context.MouseX, context.MouseY))
            Scrollbar.OnMouseWheel(context, delta);
    }
}
