// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
        private StyledProperty<bool> bold = new("Bold");
        private StyledProperty<bool> italic = new("Italic");
        private StyledProperty<bool> underline = new("Underline");
        private StyledProperty<int> fontIndex = new ("FontIndex");
        private StyledProperty<InterfaceColor?> textColor = new("TextColor");
        private StyledProperty<InterfaceColor?> textShadow = new("TextShadow");
        private StyledProperty<HorizontalAlignment> textAlignment = new("TextAlignment");

        public bool Bold
        {
            get => bold.Value;
            set
            {
                bold.Set(value);
                StyleDirty = true;
            }
        }

        public bool Italic
        {
            get => italic.Value;
            set
            {
                italic.Set(value);
                StyleDirty = true;
            }
        }

        public bool Underline
        {
            get => underline.Value;
            set
            {
                underline.Set(value);
                StyleDirty = true;
            }
        }

        public int FontIndex
        {
            get => fontIndex.Value;
            set
            {
                fontIndex.Set(value);
                StyleDirty = true;
            }
        }

        public InterfaceColor? TextColor
        {
            get => textColor.Value;
            set
            {
                textColor.Set(value);
                StyleDirty = true;
            }
        }

        public InterfaceColor? TextShadow
        {
            get => textShadow.Value;
            set
            {
                textShadow.Set(value);
                StyleDirty = true;
            }
        }

        public HorizontalAlignment TextAlignment
        {
            get => textAlignment.Value;
            set
            {
                textAlignment.Set(value);
                StyleDirty = true;
            }
        }

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

        record BuiltInfocard(BuiltRichText RichText, int Width, Infocard Infocard, InfocardDisplayStyle Style);

        private BuiltInfocard? currLeft;
        private BuiltInfocard? currRight;

        public Scrollbar Scrollbar { get; set; } = new();

        private string? setString = null;
        private string? setFont = null;
        private int setSize = 0;

        private InfocardDisplayStyle displayStyle = InfocardDisplayStyle.Default;

        protected override ElementStyle OnRestyle(UiContext context)
        {
            var infocardStyle = new StyleResolver()
                .Add(context.Data.Stylesheet?.Styles.DefaultStyle<InfocardStyle>())
                .Add(Style)
                .Add(WidthProperty)
                .Add(HeightProperty)
                .Add(bold)
                .Add(italic)
                .Add(underline)
                .Add(fontIndex)
                .Add(textColor)
                .Add(textShadow)
                .Add(textAlignment)
                .Create<InfocardStyle>();
            displayStyle = new()
            {
                Bold = infocardStyle.Bold,
                Italic = infocardStyle.Italic,
                Underline = infocardStyle.Underline,
                FontIndex = infocardStyle.FontIndex,
                Color = infocardStyle.TextColor?.GetColor(0) ?? Color4.White,
                TextShadow = infocardStyle.TextShadow == null ? default : new OptionalColor(infocardStyle.TextShadow.GetColor(0)),
                Alignment = CastAlign(infocardStyle.TextAlignment)
            };
            return infocardStyle;
        }

        public void SetString(string str) => Infocard = new() { Nodes = [new InfocardTextNode() { Contents = str }] };

        public void SetString(string str, string font, int size) => Infocard = new()
        {
            Nodes = [new InfocardTextNode() { Contents = str, ManualFont = new(font, size) }]
        };

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
                if (ic.Nodes.Count > 0)
                    ic.Nodes.Add(new InfocardParagraphNode());
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
            if (built?.Infocard != infocard || built?.Width != myRect.Width ||
                built?.Style != displayStyle)
            {
                built?.RichText?.Dispose();
                built = new(
                    rte.BuildText(infocard.CreateDisplayNodes(displayStyle, context.Data.Fonts), myRect.Width,
                        (context.ViewportHeight / 480) * 0.5f),
                    myRect.Width, infocard, displayStyle);
                CalculateScrollbar(myRect.Height);
            }

            if (drawList.PushClip(myRect))
            {
                int y = myRect.Y;

                if (Scrollbar.Visible)
                {
                    y -= (int)(Scrollbar.ScrollOffset * (built.RichText.Height - myRect.Height));
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
