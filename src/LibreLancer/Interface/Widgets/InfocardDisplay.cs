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
        
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            Background?.Draw(context, myRectangle);
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
                }
                context.Renderer2D.DrawWithClip(myRect, () =>
                {
                    rte.RenderText(richText, myRect.X, myRect.Y);
                });
            }
            Border?.Draw(context, myRectangle);
        }
    }
}