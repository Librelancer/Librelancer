// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
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
                setString = null;
                setFont = null;
                setSize = 0;
                currLeft?.RichText.Dispose();
                currLeft = null;
                currRight?.RichText.Dispose();
                currRight = null;
            }
        }
        private Infocard? infocardRight;

        record InfocardStyle(Color4? TextColor, Color4? TextShadow, bool BoldFirstLine,
            Color4? FirstLineColor, float FirstLineScale, bool CenterFirstLine, float FontScale);

        record BuiltInfocard(BuiltRichText RichText, int Width, Infocard Infocard, InfocardStyle Style);

        private BuiltInfocard? currLeft;
        private BuiltInfocard? currRight;

        public InterfaceColor? TextColor { get; set; }
        public InterfaceColor? TextShadow { get; set; }
        public float FontScale { get; set; } = 0.5f;
        public float ColumnSplit { get; set; } = 0.5f;
        public bool BoldFirstLine { get; set; }
        public bool BoldFirstColumnLine { get; set; }
        public InterfaceColor? FirstLineColor { get; set; }
        public InterfaceColor? FirstColumnLineColor { get; set; }
        public float FirstLineScale { get; set; } = 1f;
        public float FirstColumnLineScale { get; set; } = 1f;
        public bool CenterFirstLine { get; set; }
        public bool CenterFirstColumnLine { get; set; }
        public bool CenterAdditionalInfocardFirstLine { get; set; }

        public Scrollbar Scrollbar { get; set; } = new();

        private string? setString = null;
        private string? setFont = null;
        private int setSize = 0;

        public void SetString(string str)
        {
            this.setString = str;
            this.setFont = null;
            this.setSize = 0;
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
            var hasInfocard = false;
            foreach (var i in infocards)
            {
                if (i == null)
                    continue;

                if (hasInfocard && ic.Nodes.Count > 0 && ic.Nodes[^1] is not RichTextParagraphNode)
                    ic.Nodes.Add(new RichTextParagraphNode());

                var firstText = hasInfocard && CenterAdditionalInfocardFirstLine;
                foreach (var node in i.Nodes)
                {
                    if (firstText && node is RichTextTextNode text && !string.IsNullOrWhiteSpace(text.Contents))
                    {
                        ic.Nodes.Add(new RichTextTextNode
                        {
                            Bold = text.Bold,
                            Italic = text.Italic,
                            Underline = text.Underline,
                            FontName = text.FontName,
                            FontSize = text.FontSize * Math.Max(FirstLineScale, 0),
                            Color = text.Color,
                            Shadow = text.Shadow,
                            Background = text.Background,
                            Alignment = TextAlignment.Center,
                            Contents = text.Contents
                        });
                        firstText = false;
                    }
                    else
                    {
                        ic.Nodes.Add(node);
                    }
                }
                hasInfocard = true;
            }
            Infocard = ic;
        }

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);
            Scrollbar.OnLayout(context, new Layout(ClientRectangle), delta);
        }

        private IList<RichTextNode> GetRenderNodes(Infocard infocard, InfocardStyle style)
        {
            var styleFirstLine = style.BoldFirstLine || style.FirstLineColor != null ||
                                 Math.Abs(style.FirstLineScale - 1f) > 0.0001f || style.CenterFirstLine;
            if (style.TextColor == null && style.TextShadow == null && !styleFirstLine)
                return infocard.Nodes;

            var nodes = new List<RichTextNode>(infocard.Nodes.Count);
            var firstTextPending = styleFirstLine;
            foreach (var node in infocard.Nodes)
            {
                if (node is not RichTextTextNode text)
                {
                    nodes.Add(node);
                    continue;
                }

                var bold = text.Bold;
                var firstText = false;
                if (firstTextPending && !string.IsNullOrWhiteSpace(text.Contents))
                {
                    if (style.BoldFirstLine)
                        bold = true;
                    firstTextPending = false;
                    firstText = true;
                }

                var textColor = firstText && style.FirstLineColor.HasValue
                    ? style.FirstLineColor.Value
                    : style.TextColor ?? text.Color;
                var fontSize = firstText ? text.FontSize * Math.Max(style.FirstLineScale, 0) : text.FontSize;
                var alignment = firstText && style.CenterFirstLine ? TextAlignment.Center : text.Alignment;

                nodes.Add(new RichTextTextNode
                {
                    Bold = bold,
                    Italic = text.Italic,
                    Underline = text.Underline,
                    FontName = text.FontName,
                    FontSize = fontSize,
                    Color = textColor,
                    Shadow = style.TextShadow.HasValue ? new OptionalColor(style.TextShadow.Value) : text.Shadow,
                    Background = text.Background,
                    Alignment = alignment,
                    Contents = text.Contents
                });
            }

            return nodes;
        }

        void DrawInfocard(DrawList2D drawList, UiContext context, Infocard infocard, Rectangle myRect,
            bool boldFirstLine, InterfaceColor? firstLineColor, float firstLineScale, bool centerFirstLine,
            ref BuiltInfocard? built)
        {
            var rte = context.RenderContext.Renderer2D.RichText;
            var style = new InfocardStyle(
                TextColor?.GetColor(context.GlobalTime),
                TextShadow?.GetColor(context.GlobalTime),
                boldFirstLine,
                firstLineColor?.GetColor(context.GlobalTime),
                firstLineScale,
                centerFirstLine,
                (context.ViewportHeight / 480) * Math.Max(FontScale, 0));
            if (built?.Infocard != infocard || built?.Width != myRect.Width || built.Style != style)
            {
                built?.RichText?.Dispose();
                built = new(rte.BuildText(GetRenderNodes(infocard, style), myRect.Width, style.FontScale),
                    myRect.Width, infocard, style);
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
                var text = setString;
                var font = setFont;
                var size = setSize;
                setString = null;
                setFont = null;
                setSize = 0;
                Infocard = new Infocard() { Nodes = [] };
                string fontName = font ?? "$ListText";
                if (fontName[0] == '$') fontName = context.Data.Fonts.ResolveNickname(fontName.Substring(1));

                foreach (var s in text.Split('\n'))
                {
                    Infocard.Nodes.Add(new RichTextTextNode()
                    {
                        Contents = s,
                        FontName = fontName,
                        FontSize = size < 1 ? 22 : size,
                        Alignment = TextAlignment.Left,
                        Color = context.Data.GetColor("text").Color,
                        Shadow = new OptionalColor(context.Data.GetColor("black").Color)
                    });
                    Infocard.Nodes.Add(new RichTextParagraphNode());
                }

            }

            if (!Visible) return;
            Background?.Draw(context, drawList, ClientRectangle);
            var myRectangle = ClientRectangle;
            myRectangle.Width -= Scrollbar.ClientRectangle.Width;

            if (Infocard != null)
            {
                if (infocardRight != null)
                {
                    var split = Math.Clamp(ColumnSplit, 0.1f, 0.9f);
                    var lRect = myRectangle with { Width = myRectangle.Width * split };
                    var rRect = myRectangle with
                    {
                        X = lRect.X + lRect.Width,
                        Width = myRectangle.Width - lRect.Width
                    };
                    DrawInfocard(drawList, context, Infocard, context.PointsToPixels(lRect),
                        BoldFirstLine || BoldFirstColumnLine,
                        FirstLineColor ?? FirstColumnLineColor,
                        BoldFirstLine ? FirstLineScale : FirstColumnLineScale,
                        BoldFirstLine ? CenterFirstLine : CenterFirstColumnLine,
                        ref currLeft);
                    DrawInfocard(drawList, context, infocardRight, context.PointsToPixels(rRect),
                        BoldFirstLine, FirstLineColor, FirstLineScale, CenterFirstLine, ref currRight);
                }
                else
                {
                    var myRect = context.PointsToPixels(myRectangle);
                    DrawInfocard(drawList, context, Infocard, myRect,
                        BoldFirstLine || BoldFirstColumnLine,
                        FirstLineColor ?? FirstColumnLineColor,
                        BoldFirstLine ? FirstLineScale : FirstColumnLineScale,
                        BoldFirstLine ? CenterFirstLine : CenterFirstColumnLine,
                        ref currLeft);
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
