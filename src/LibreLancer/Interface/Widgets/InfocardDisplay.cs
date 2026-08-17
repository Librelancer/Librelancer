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
        public Infocard? Infocard
        {
            get;
            set
            {
                field = value;
                infocardRight = null;
            }
        }
        private Infocard? infocardRight;

        record BuiltInfocard(BuiltRichText RichText, int Width, Infocard Infocard);

        private BuiltInfocard? currLeft;
        private BuiltInfocard? currRight;

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

        public void SetColumnInfocards(Infocard left, Infocard right)
        {
            Infocard = left;
            infocardRight = right;
        }

        public void SetInfocards(Infocard?[] infocards)
        {
            var ic = new Infocard();
            foreach (var i in infocards)
            {
                if (i == null)
                    continue;
                if(ic.Nodes.Count > 0)
                    ic.Nodes.Add(new RichTextParagraphNode());
                ic.Nodes.AddRange(i.Nodes);
            }
            Infocard = ic;
        }

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);
            Scrollbar.OnLayout(context, new Layout(ClientRectangle), delta);
        }

        void DrawInfocard(DrawList2D drawList, UiContext context, Infocard infocard, Rectangle myRect,
            ref BuiltInfocard? built)
        {
            var rte = context.RenderContext.Renderer2D.RichText;
            if (built?.Infocard != infocard || built?.Width != myRect.Width)
            {
                built?.RichText?.Dispose();
                built = new(rte.BuildText(infocard.Nodes, myRect.Width, (context.ViewportHeight / 480) * 0.5f),
                    myRect.Width, infocard);
                CalculateScrollbar(myRect.Height);
            }
            if (drawList.PushClip(myRect))
            {
                int y = myRect.Y;

                if (Scrollbar.Visible)
                {
                    y -= (int) (Scrollbar.ScrollOffset * (built.RichText.Height - myRect.Height));
                }

                rte.RenderText(drawList, built.RichText, myRect.X, y);
                drawList.PopClip();
            }
        }

        void CalculateScrollbar(int containerHeight)
        {
            var l = currLeft?.RichText.Height ?? 0;
            var r = currRight?.RichText.Height ?? 0;
            var height = float.MaxNative(l, r);
            if (height > containerHeight + 1)
            {
                Scrollbar.ScrollOffset = 0;
                Scrollbar.ThumbSize = containerHeight / height;
                const float TICK_MAGIC = 0.2627986f;
                Scrollbar.Tick = 0.01f * (Scrollbar.ThumbSize / TICK_MAGIC);
                Scrollbar.Visible = true;
            }
            else
            {
                Scrollbar.Visible = false;
            }
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
                if (infocardRight != null)
                {
                    var lRect = myRectangle with { Width = myRectangle.Width * 0.5f };
                    var rRect = myRectangle with
                    {
                        X = lRect.X + lRect.Width,
                        Width = lRect.Width
                    };
                    DrawInfocard(drawList, context, Infocard, context.PointsToPixels(lRect), ref currLeft);
                    DrawInfocard(drawList, context, infocardRight, context.PointsToPixels(rRect), ref currRight);
                }
                else
                {
                    var myRect = context.PointsToPixels(myRectangle);
                    DrawInfocard(drawList, context, Infocard, myRect, ref currLeft);
                }
                Scrollbar.Render(context, delta, drawList);
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
