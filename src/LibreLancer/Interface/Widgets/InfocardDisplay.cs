using LibreLancer.Infocards;

namespace LibreLancer.Interface
{
    [UiLoadable]
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

        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            return myRectangle;
        }
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myRectangle = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, myRectangle);
            myRectangle.Width -= scrollbar.Style.Width;
            if (Infocard != null)
            {
                context.Mode2D();
                var rte = context.Renderer2D.CreateRichTextEngine();
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
                        scrollbar.Tick = (float)(3 * (1.0 / richText.Height));
                        scrollbarVisible = true;
                    } else {
                        scrollbarVisible = false;
                    }
                   
                }
                if(scrollbarVisible)
                    scrollbar.Render(context, new RectangleF(myRectangle.X + myRectangle.Width, myRectangle.Y, scrollbar.Style.Width, myRectangle.Height));
                context.Renderer2D.DrawWithClip(myRect, () =>
                {
                    int y = myRect.Y;
                    if (scrollbarVisible) {
                        y -= (int) (scrollbar.ScrollOffset * (richText.Height - myRect.Height));
                    }
                    rte.RenderText(richText, myRect.X, y);
                });
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
    }
}