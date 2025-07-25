using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using BlurgText;

namespace LibreLancer.Graphics.Text;

class BlurgEngine : RichTextEngine
{
    class BlurgRenderCache : CachedRenderString
    {
        public BlurgResult Result;
    }
    class BlurgBuiltText : BuiltRichText
    {
        public BlurgResult Result;
        public BlurgFormattedText[] Paragraphs;
        public int[] NodeOffsets;
        public Blurg Parent;

        int width = -1;
        public override void Recalculate(float width)
        {
            if ((int)width == this.width)
                return;
            Result.Dispose();
            Result = Parent.BuildFormattedText(Paragraphs, true, width);
            this.width = (int)width;
        }

        public override float Height => Result.Height;
        public override Rectangle GetCaretPosition(int layoutIndex, int textPosition)
        {
            if (Result.Cursors.Length == 0) {
                return new Rectangle(0, 0, 1, (int)Paragraphs[0].DefaultFont.LineHeight(Paragraphs[0].DefaultSize));
            }
            var pos = NodeOffsets[layoutIndex] + textPosition;
            if (pos == -1) {
                return new Rectangle(0, 0, 1, Result.Cursors[0].Height);
            }
            pos = MathHelper.Clamp(pos, 0, Result.Cursors.Length);
            var cur = Result.Cursors[pos];
            return new Rectangle(cur.X, cur.Y, 1, (int)cur.Height);
        }

        public override void Dispose()
        {
            Result.Dispose();
            Parent = null;
            Paragraphs = null;
        }
    }

    private Blurg blurg;
    private RenderContext context;
    private Renderer2D renderer;
    HashSet<string> loadedTtfs = new(StringComparer.OrdinalIgnoreCase);

    private Texture2D[] textures;
    private int nextTex = 0;

    IntPtr AllocateTexture(int width, int height)
    {
        textures[nextTex] = new Texture2D(context, width, height);
        nextTex++;
        return (IntPtr)(nextTex - 1);
    }

    private void TextureUpdate(IntPtr userdata, IntPtr buffer, int x, int y, int width, int height)
    {
        var tex = textures[(int)userdata];
        tex.SetData(0, new Rectangle(x,y,width,height), buffer);
    }

    public BlurgEngine(RenderContext context, Renderer2D renderer)
    {
        textures = new Texture2D[32];
        this.context = context;
        this.renderer = renderer;
        blurg = new Blurg(AllocateTexture, TextureUpdate);
        blurg.EnableSystemFonts();
    }

    public override void AddTtfFile(string id, ReadOnlySpan<byte> data)
    {
        if (!loadedTtfs.Add(id))
            return;
        blurg.AddFontFromMemory(data);
    }


    public override void Dispose()
    {
        blurg.Dispose();
        for (int i = 0; i < nextTex; i++) {
            textures[i].Dispose();
        }
    }

    void DrawResult(BlurgResult r, int x, int y)
    {
        for (int i = 0; i < r.Count; i++)
        {
            var g = r[i];
            var tex = textures[(int)g.UserData];
            renderer.Draw(tex, new TexSource(
                    new(g.U0, g.V0),
                    new(g.U1, g.V0),
                    new(g.U0, g.V1),
                    new(g.U1, g.V1)),
                new Rectangle(x + g.X, y + g.Y, g.Width, g.Height),
                (Color4)(new VertexDiffuse() { Pixel = g.Color.Value }));
        }
    }

    public override void RenderText(BuiltRichText txt, int x, int y)
    {
        var bt = (BlurgBuiltText)txt;
        DrawResult(bt.Result, x, y);
    }

    static BlurgColor Col(Color4 a) => new (){ Value = (VertexDiffuse)a };

    static BlurgAlignment Align(TextAlignment ta) => ta switch
    {
        TextAlignment.Center => BlurgAlignment.Center,
        TextAlignment.Right => BlurgAlignment.Right,
        _ => BlurgAlignment.Left,
    };

    public override BuiltRichText BuildText(IList<RichTextNode> nodes, int width, float sizeMultiplier = 1)
    {
        var fmt = ArrayPool<BlurgFormattedText>.Shared.Rent(nodes.Count);
        int fmtIdx = 0;

        StringBuilder currentBuilder = new StringBuilder();
        List<BlurgStyleSpan> spans = new List<BlurgStyleSpan>();

        BlurgFont defaultFont = null;
        var defaultSize = -1f;
        BlurgColor defaultColor = BlurgColor.Black;
        BlurgColor defaultBackground = new BlurgColor(0, 0, 0, 0);
        BlurgUnderline defaultUnderline = default;
        BlurgShadow defaultShadow = default;
        BlurgAlignment align = BlurgAlignment.Left;

        void AddFormatted()
        {
            if (currentBuilder.Length > 0)
            {
                if (defaultFont == null)
                {
                    defaultFont = spans.Count > 0
                        ? spans[0].Font
                        : blurg.QueryFont("Arial", FontWeight.Regular, false)!;
                }
                if (defaultSize <= 0)
                {
                    defaultSize = spans.Count > 0
                        ? spans[0].FontSize
                        : (24 * sizeMultiplier);
                    if (spans.Count > 0)
                    {
                        defaultColor = spans[0].Color;
                        defaultUnderline = spans[0].Underline;
                        defaultShadow = spans[0].Shadow;
                        defaultBackground = spans[0].Background;
                    }
                }
                var txt = new BlurgFormattedText(currentBuilder.ToString(), defaultFont);
                txt.DefaultSize = defaultSize;
                txt.DefaultColor = defaultColor;
                txt.DefaultUnderline = defaultUnderline;
                txt.DefaultShadow = defaultShadow;
                txt.Alignment = align;
                txt.Spans = spans.ToArray();
                fmt[fmtIdx++] = txt;
            }
            if (spans.Count > 0)
            {
                defaultFont = spans[^1].Font;
                defaultColor = spans[^1].Color;
                defaultBackground = spans[^1].Background;
                defaultSize = spans[^1].FontSize;
                defaultUnderline = spans[^1].Underline;
                defaultShadow = spans[^1].Shadow;
            }
            currentBuilder = new StringBuilder();
            spans = new List<BlurgStyleSpan>();
        }

        int totalLength = 0;
        int[] offsets = new int[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            offsets[i] = totalLength;
            if (nodes[i] is RichTextParagraphNode)
            {
                if (spans.Count > 0) {
                    var s = spans[^1];
                    s.EndIndex++;
                    spans[^1] = s;
                }
                currentBuilder.AppendLine();
                totalLength++;
            }
            else if (nodes[i] is RichTextTextNode text && text.Contents.Length > 0)
            {
                var ta = Align(text.Alignment);
                if (currentBuilder.Length > 0 &&
                    ta != align)
                {
                    AddFormatted();
                }
                align = ta;
                var font = blurg.QueryFont(text.FontName, text.Bold ? FontWeight.Bold : FontWeight.Regular,
                    text.Italic)!;
                spans.Add(new BlurgStyleSpan()
                {
                    Font = font,
                    FontSize = text.FontSize * sizeMultiplier,
                    Color = Col(text.Color),
                    Background = text.Background.Enabled ? Col(text.Background.Color) : new BlurgColor(0,0,0,0),
                    Shadow = text.Shadow.Enabled ? new BlurgShadow() { Color = Col(text.Shadow.Color), Pixels = 2 } : default,
                    StartIndex = currentBuilder.Length,
                    EndIndex = currentBuilder.Length + (text.Contents.Length - 1)
                });
                currentBuilder.Append(text.Contents);
                totalLength += text.Contents.Length;
            }
        }
        AddFormatted();

        var built = new BlurgFormattedText[fmtIdx];
        for (int i = 0; i < fmtIdx; i++) {
            built[i] = fmt[i];
        }
        ArrayPool<BlurgFormattedText>.Shared.Return(fmt);

        var result = blurg.BuildFormattedText(built, true, width < 0 ? 0 : width);

        var bt = new BlurgBuiltText() { Paragraphs = built, Parent = blurg, NodeOffsets = offsets, Result = result };
        return bt;
    }

    public override void DrawStringBaseline(string fontName, float size, string text, float x, float y, Color4 color,
        bool underline = false, OptionalColor shadow = default)
    {
        var f = blurg.QueryFont(fontName, FontWeight.Regular, false)!;
        var bt = new BlurgFormattedText(text, f);
        bt.DefaultSize = size;
        bt.DefaultColor = Col(color);
        if (underline)
            bt.DefaultUnderline = new BlurgUnderline() { Enabled = true };
        if (shadow.Enabled)
            bt.DefaultShadow = new BlurgShadow() { Color = Col(shadow.Color), Pixels = 2 };
        using (var r = blurg.BuildFormattedText(bt)) {
            DrawResult(r, (int)x, (int)y);
        }
    }

    public override Point MeasureString(string fontName, float size, string text)
    {
        var f = blurg.QueryFont(fontName, FontWeight.Regular, false)!;
        var res = blurg.MeasureString(f, size, text);
        return new Point((int)res.X, (int)res.Y);
    }

    public override float LineHeight(string fontName, float size)
    {
        return blurg.QueryFont(fontName, FontWeight.Regular, false)!.LineHeight(size);
    }

    void UpdateCache(ref CachedRenderString cache, string fontName, float size, string text, bool underline,
        TextAlignment alignment, bool shadow, float maxWidth)
    {
        if (cache == null)
        {
            cache = new BlurgRenderCache()
            {
                FontName = fontName, FontSize = size, Text = text, Underline = underline,
                Alignment = alignment, MaxWidth = maxWidth
            };
        }
        if (cache is not BlurgRenderCache pc) throw new ArgumentException("cache");
        if (pc.Result == null || pc.Update(fontName, text, size, underline, alignment, shadow, maxWidth))
        {
            var pixels = size * (96.0f / 72.0f);
            var fnt = blurg.QueryFont(fontName, FontWeight.Regular, false)!;
            pc.Result?.Dispose();
            var fmt = new BlurgFormattedText(text, fnt);
            fmt.DefaultSize = pixels;
            fmt.DefaultColor = new BlurgColor(255, 0, 0, 255);
            fmt.DefaultUnderline = underline ? new BlurgUnderline() { Enabled = true } : default;
            fmt.Alignment = Align(alignment);
            fmt.DefaultShadow = shadow ? new BlurgShadow{ Color = new BlurgColor(128, 0, 0, 255), Pixels = 2 } : default;
            pc.Result = blurg.BuildFormattedText(fmt, false, maxWidth);
        }
    }

    public override void DrawStringCached(ref CachedRenderString cache, string fontName, float size, string text, float x, float y,
        Color4 color, bool underline = false, OptionalColor shadow = default, TextAlignment alignment = TextAlignment.Left,
        float maxWidth = 0)
    {
        UpdateCache(ref cache, fontName, size, text, underline, alignment, shadow.Enabled, maxWidth);
        var pc = (BlurgRenderCache)cache;
        for (int i = 0; i < pc.Result.Count; i++)
        {
            var g = pc.Result[i];
            var tex = textures[(int)g.UserData];
            renderer.Draw(tex, new TexSource(
                    new(g.U0, g.V0),
                    new(g.U1, g.V0),
                    new(g.U0, g.V1),
                    new(g.U1, g.V1)),
                new Rectangle((int)(x + g.X), (int)(y + g.Y), g.Width, g.Height),
                g.Color.R == 255 ? color : shadow.Color);
        }
    }

    public override Point MeasureStringCached(ref CachedRenderString cache, string fontName, float size, float maxWidth, string text,
        bool underline, bool shadow, TextAlignment alignment)
    {
        UpdateCache(ref cache, fontName, size, text, underline, alignment, shadow, maxWidth);
        var pc = (BlurgRenderCache)cache;
        return new Point((int)pc.Result.Width, (int)pc.Result.Height);
    }
}
