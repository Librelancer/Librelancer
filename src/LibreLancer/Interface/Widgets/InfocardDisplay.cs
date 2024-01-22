// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Graphics.Text;
using LibreLancer.Infocards;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class InfocardDisplay : UiWidget
    {
        public Infocard Infocard { get; set; }
        private BuiltRichText richText;
        private int mW = -1;
        private Infocard currInfocard;

        private Scrollbar scrollbar = new Scrollbar() {Smooth = true};
        private bool scrollbarVisible = false;

        public override void ApplyStylesheet(Stylesheet sheet)
        {
            scrollbar.ApplyStyle(sheet);
        }

        private string setString = null;
        private string setFont = null;
        private int setSize = 0;
        public void SetString(string str)
        {
            this.setString = str;
        }

        public void SetString(string str, string font, int size)
        {
            this.setString = str;
            this.setFont = font;
            this.setSize = size;
        }

        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            return myRectangle;
        }
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            //TODO: fix up
            if (setString != null)
            {
                Infocard = new Infocard() {Nodes = new List<RichTextNode>()};
                string fontName = setFont ?? "$ListText";
                if (fontName[0] == '$') fontName = context.Data.Fonts.ResolveNickname(fontName.Substring(1));
                foreach (var s in setString.Split('\n')) {
                    Infocard.Nodes.Add(new RichTextTextNode()
                    {
                        Contents = s,
                        FontName = fontName,
                        FontSize = setSize < 1 ? 22 : setSize,
                        Alignment = TextAlignment.Left,
                        Color = context.Data.GetColor("text").Color,
                        Shadow = new OptionalColor(context.Data.GetColor("black").Color)
                    });
                    Infocard.Nodes.Add(new RichTextParagraphNode());
                }
                setString = null;
                setFont = null;
                setSize = 0;
            }
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, myRectangle);
            myRectangle.Width -= scrollbar.Style.Width;
            if (Infocard != null)
            {
                var rte = context.RenderContext.Renderer2D.CreateRichTextEngine();
                var myRect = context.PointsToPixels(myRectangle);
                if (currInfocard != Infocard || mW != myRect.Width)
                {
                    richText?.Dispose();
                    currInfocard = Infocard;
                    mW = myRect.Width;
                    richText = rte.BuildText(Infocard.Nodes, mW, (context.ViewportHeight / 480) * 0.5f);
                    var h = richText.Height;
                    if ((int) h > myRect.Height + 2) {
                        scrollbar.ScrollOffset = 0;
                        scrollbar.ThumbSize = myRect.Height / h;
                        const float TICK_MAGIC = 0.2627986f;
                        scrollbar.Tick = 0.01f * (scrollbar.ThumbSize / TICK_MAGIC);
                        scrollbarVisible = true;
                    } else {
                        scrollbarVisible = false;
                    }

                }
                if(scrollbarVisible)
                    scrollbar.Render(context, new RectangleF(myRectangle.X + myRectangle.Width, myRectangle.Y, scrollbar.Style.Width, myRectangle.Height));
                context.RenderContext.ScissorEnabled = true;
                context.RenderContext.ScissorRectangle = myRect;
                int y = myRect.Y;
                if (scrollbarVisible) {
                    y -= (int) (scrollbar.ScrollOffset * (richText.Height - myRect.Height));
                }
                rte.RenderText(richText, myRect.X, y);
                context.RenderContext.ScissorEnabled = false;
            }
            Border?.Draw(context, myRectangle);
        }

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            var myRectangle = GetMyRectangle(context, parentRectangle);
            if(Infocard != null && scrollbarVisible)
                scrollbar.OnMouseDown(context, myRectangle);
        }

        public override void OnMouseUp(UiContext context, RectangleF parentRectangle)
        {
            var myRectangle = GetMyRectangle(context, parentRectangle);
            if (Infocard != null && scrollbarVisible)
                scrollbar.OnMouseUp(context, myRectangle);
        }

        public override void OnMouseWheel(UiContext context, RectangleF parentRectangle, float delta)
        {
            var myRectangle = GetMyRectangle(context, parentRectangle);
            if (Infocard != null && scrollbarVisible &&
                myRectangle.Contains(context.MouseX, context.MouseY))
                scrollbar.OnMouseWheel(delta);
        }
    }
}
