// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using LibreLancer.Infocards;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class InfocardDisplay : UiWidget
    {
        public Infocard? Infocard { get; set; }
        private BuiltRichText? richText;
        private int mW = -1;
        private Infocard? currInfocard;

        public Scrollbar Scrollbar { get; set; } = new();

        private string? setString = null;
        private string? setFont = null;
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

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);
            Scrollbar.OnLayout(context, new Layout(ClientRectangle), delta);
        }


        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            // TODO: fix up
            if (setString != null)
            {
                Infocard = new Infocard() { Nodes = [] };
                string fontName = setFont ?? "$ListText";
                if (fontName[0] == '$') fontName = context.Data.Fonts.ResolveNickname(fontName.Substring(1));

                foreach (var s in setString.Split('\n'))
                {
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
            Background?.Draw(context, drawList, ClientRectangle);
            var myRectangle = ClientRectangle;
            myRectangle.Width -= Scrollbar.ClientRectangle.Width;

            if (Infocard != null)
            {
                var rte = context.RenderContext.Renderer2D.RichText;
                var myRect = context.PointsToPixels(myRectangle);

                if (currInfocard != Infocard || mW != myRect.Width)
                {
                    richText?.Dispose();
                    currInfocard = Infocard;
                    mW = myRect.Width;
                    richText = rte.BuildText(Infocard.Nodes, mW, (context.ViewportHeight / 480) * 0.5f);
                    var h = richText.Height;

                    if ((int) h > myRect.Height + 2)
                    {
                        Scrollbar.ScrollOffset = 0;
                        Scrollbar.ThumbSize = myRect.Height / h;
                        const float TICK_MAGIC = 0.2627986f;
                        Scrollbar.Tick = 0.01f * (Scrollbar.ThumbSize / TICK_MAGIC);
                        Scrollbar.Visible = true;
                    }
                    else
                    {
                        Scrollbar.Visible = false;
                    }

                }

                Scrollbar.Render(context, delta, drawList);

                if (drawList.PushClip(myRect))
                {
                    int y = myRect.Y;

                    if (Scrollbar.Visible)
                    {
                        y -= (int) (Scrollbar.ScrollOffset * (richText!.Height - myRect.Height));
                    }

                    rte.RenderText(drawList, richText!, myRect.X, y);
                    drawList.PopClip();
                }
            }

            Border?.Draw(context, drawList, ClientRectangle);
        }

        public override void Update(UiContext context, double delta)
        {
            base.Update(context, delta);
            Scrollbar.Update(context, delta);
        }

        public override void OnMouseDown(UiContext context)
        {
            if (Infocard != null)
                Scrollbar.OnMouseDown(context);
        }

        public override void OnMouseUp(UiContext context)
        {
            if (Infocard != null)
                Scrollbar.OnMouseUp(context);
        }

        public override void OnMouseWheel(UiContext context, float delta)
        {
            if (Infocard != null && ClientRectangle.Contains(context.MouseX, context.MouseY))
                Scrollbar.OnMouseWheel(context, delta);
        }
    }
}
