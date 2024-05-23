// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LibreLancer.Platforms.Win32;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using DWrite = SharpDX.DirectWrite;
using D2D1 = SharpDX.Direct2D1;

namespace LibreLancer.Graphics.Text.DirectWrite
{
    class ColorDrawingEffect : ComObject
    {
        public Color4 Color { get; set; }
        public OptionalColor Shadow { get; set; }
        public OptionalColor Background { get; set; }
        public ColorDrawingEffect(Color4 color, OptionalColor shadow, OptionalColor background)
        {
            Color = color;
            Shadow = shadow;
            Background = background;
        }
    }

    class DirectWriteBuiltText : BuiltRichText
    {
        float height = 1f;
        public List<TextLayout> Layout;
        public Dictionary<int, (int layoutIndex, int offset)> Offsets;
        public int Width;

        public override float Height => height;
        private bool disposed = false;

        public override void Dispose()
        {
            disposed = true;
            foreach (var l in Layout) l.Dispose();
        }

        ~DirectWriteBuiltText()
        {
            if (!disposed) Dispose();
        }

        public override void Recalculate(float width)
        {
            if (Math.Abs((int)width - Width) < 2)
                return;
            Width = (int)width;
            foreach(var layout in Layout) {
                layout.MaxWidth = width;
            }
            CacheQuads();
        }

        DirectWriteText engine;


        public DirectWriteBuiltText(DirectWriteText engine)
        {
            this.engine = engine;
        }

        public DrawQuad[] Quads;
        public void CacheQuads()
        {
            float heightOffset = 0;
            foreach (var layout in Layout)
            {
                layout.Draw(engine.Renderer, 0, heightOffset);
                heightOffset += layout.Metrics.Height;
            }
            this.height = heightOffset;
            Quads = engine.Renderer.Quads.ToArray();
            engine.Renderer.Quads = new List<DrawQuad>();
        }


        public override Rectangle GetCaretPosition(int nodeIndex, int textPosition)
        {
            var (layoutIndex, textOffset) = Offsets[nodeIndex];
            var metrics = Layout[layoutIndex].HitTestTextPosition(textOffset + textPosition, false, out var caretX, out var caretY);
            int x = (int)Layout[layoutIndex].Metrics.Left;
            int y = (int) Layout[layoutIndex].Metrics.Top;
            return new Rectangle(x + (int) caretX, y + (int)caretY, 2, (int) metrics.Height);
        }
    }
    class DirectWriteText : RichTextEngine
    {
        Factory dwFactory;
        public DirectWriteTextRenderer Renderer;
        public FontCollection customCollection;
        CustomFontLoader customLoader;
        Renderer2D render2d;
        public DirectWriteText(RenderContext context, Renderer2D r2d)
        {
            dwFactory = new Factory();
            Renderer = new DirectWriteTextRenderer(context, dwFactory, this);
            render2d = r2d;
            CreateCustomCollection();
        }

        void CreateCustomCollection()
        {
            customLoader = new CustomFontLoader(dwFactory);
            customCollection = new FontCollection(dwFactory, customLoader, customLoader.Key);
        }

        DWrite.TextAlignment CastAlignment(TextAlignment t)
        {
            if (t == TextAlignment.Left)
                return DWrite.TextAlignment.Leading;
            else if (t == TextAlignment.Right)
                return DWrite.TextAlignment.Trailing;
            else
                return DWrite.TextAlignment.Center;
        }

        public override BuiltRichText BuildText(IList<RichTextNode> nodes, int width, float sizeMultiplier = 1)
        {
            if(nodes.Count == 0)
                return new EmptyRichText();
            if (!customLoader.Valid) CreateCustomCollection();
            var paragraphs = new List<List<RichTextNode>>();
            paragraphs.Add(new List<RichTextNode>());
            int first = 0;
            while (nodes[first] is RichTextParagraphNode && first < nodes.Count) first++;
            DWrite.TextAlignment ta = CastAlignment((nodes[first] as RichTextTextNode).Alignment);
            paragraphs[paragraphs.Count - 1].Add(nodes[first]);
            foreach (var node in nodes.Skip(first + 1))
            {
                if (node is RichTextParagraphNode)
                    paragraphs.Add(new List<RichTextNode>());
                else
                {
                    var n = (RichTextTextNode)node;
                    var align = CastAlignment(n.Alignment);
                    if (align != ta && paragraphs[paragraphs.Count - 1].Count > 0)
                        paragraphs.Add(new List<RichTextNode>());
                    paragraphs[paragraphs.Count - 1].Add(node);
                    ta = align;
                }
            }
            string lastFont = null;
            float lastSize = 0;
            //Format text
            var layouts = new List<TextLayout>();
            var offsets = new Dictionary<int, (int layoutIndex, int offset)>();
            for (int j = 0; j < paragraphs.Count; j++)
            {
                var p = paragraphs[j];
                var builder = new StringBuilder();
                foreach (var n in p)
                {
                    builder.Append(((RichTextTextNode)n).Contents);
                }
                var layout = new TextLayout(
                    dwFactory,
                    builder.ToString(),
                    new TextFormat(
                    dwFactory,
                    string.IsNullOrEmpty(lastFont) ? "Arial" : lastFont,
                    lastSize > 0 ? lastSize : (16 * sizeMultiplier)
                    ),
                    width,
                    float.MaxValue
                );
                if(p.Count > 0)
                    layout.TextAlignment = CastAlignment(((RichTextTextNode)p[0]).Alignment);
                int startIdx = 0;
                foreach (var n in p)
                {
                    var text = (RichTextTextNode)n;
                    offsets[nodes.IndexOf(n)] = (layouts.Count, startIdx);
                    var range = new TextRange(startIdx, text.Contents.Length);
                    if (text.Bold) layout.SetFontWeight(FontWeight.Bold, range);
                    if (text.Italic) layout.SetFontStyle(FontStyle.Italic, range);
                    if (text.Underline) layout.SetUnderline(true, range);
                    if (!string.IsNullOrEmpty(text.FontName))
                    {
                        if(customCollection.FindFamilyName(text.FontName, out int _)) {
                            layout.SetFontCollection(customCollection, range);
                        }
                        layout.SetFontFamilyName(text.FontName, range);
                        lastFont = text.FontName;
                    }
                    if (text.FontSize > 0)
                    {
                        layout.SetFontSize(text.FontSize * sizeMultiplier, range);
                        lastSize = text.FontSize * sizeMultiplier;
                    }
                    layout.SetDrawingEffect(new ColorDrawingEffect(text.Color, text.Shadow, text.Background), range);
                    startIdx += text.Contents.Length;
                }
                layouts.Add(layout);
            }
            //Return
            var built = new DirectWriteBuiltText(this) { Layout = layouts, Offsets = offsets, Width = width };
            built.CacheQuads();
            return built;
        }

        public override void Dispose()
        {

        }

        TextFormat GetFormat(string fontName, float fontSize)
        {
            FontCollection collection = dwFactory.GetSystemFontCollection(false);
            if (customCollection.FindFamilyName(fontName, out int _)) collection = customCollection;
            return new TextFormat(dwFactory, fontName, collection, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, fontSize);
        }

        class Indent : ComObject, InlineObject
        {
            public float X;
            public Indent(float x)
            {
                X = x;
            }
            public InlineObjectMetrics Metrics => new InlineObjectMetrics() { Width = X };
            public OverhangMetrics OverhangMetrics => new OverhangMetrics();
            public IDisposable Shadow { get; set; }
            public void Draw(object clientDrawingContext, TextRenderer renderer, float originX, float originY, bool isSideways, bool isRightToLeft, ComObject clientDrawingEffect){}
            public void GetBreakConditions(out BreakCondition breakConditionBefore, out BreakCondition breakConditionAfter){
                breakConditionBefore = BreakCondition.CanBreak;
                breakConditionAfter = BreakCondition.CanBreak;
            }
        }

        float ConvertSize(float size)
        {
            return size / 72 * 96;
        }
        public override void DrawStringBaseline(string fontName, float size, string text, float x, float y, Color4 color, bool underline = false, OptionalColor shadow = default)
        {
            using (var layout = new TextLayout(dwFactory, text, GetFormat(fontName, ConvertSize(size)), float.MaxValue, float.MaxValue))
            {

                layout.SetDrawingEffect(new ColorDrawingEffect(color, shadow, default), new TextRange(0, text.Length));
                layout.Draw(Renderer, 0, 0);
                foreach(var q in Renderer.Quads) {
                    var d = q.Destination;
                    d.X += (int)x;
                    d.Y += (int)y;
                    if (q.Texture == null)
                        render2d.FillRectangle(d, q.Color);
                    else
                        render2d.Draw(q.Texture, q.Source, d, q.Color);
                }
                Renderer.Quads = new List<DrawQuad>();
            }
        }

        class DirectWriteCachedText : CachedRenderString
        {
            public DrawQuad[] quads;
            public Point size;
        }

        void UpdateCache(ref CachedRenderString cache, string fontName, float size, string text, bool underline, TextAlignment alignment, float maxWidth)
        {
            if (cache == null)
            {
                cache = new DirectWriteCachedText()
                {
                    FontName = fontName, FontSize = size, Text = text, Underline = underline,
                    MaxWidth = maxWidth,
                    Alignment = alignment
                };
            }
            if (cache is not DirectWriteCachedText pc) throw new ArgumentException("cache");
            if (pc.quads == null || pc.Update(fontName, text, size, underline, alignment, maxWidth))
            {
                using (var layout = new TextLayout(dwFactory, text, GetFormat(fontName, ConvertSize(size)), maxWidth > 0 ? maxWidth : float.MaxValue, float.MaxValue))
                {
                    if (alignment != TextAlignment.Left ||
                        maxWidth > 0) {
                        layout.MaxWidth = maxWidth > 0 ? maxWidth : layout.Metrics.Width;
                    }
                    layout.TextAlignment = CastAlignment(alignment);
                    layout.SetDrawingEffect(new ColorDrawingEffect(Color4.White, new OptionalColor(), new OptionalColor()), new TextRange(0, text.Length));
                    layout.Draw(Renderer, 0, 0);
                    pc.quads = Renderer.Quads.ToArray();
                    Renderer.Quads = new List<DrawQuad>();
                    var metrics = layout.Metrics;
                    pc.size = new Point((int)metrics.WidthIncludingTrailingWhitespace, (int)metrics.Height);
                }
            }
        }

        public override Point MeasureStringCached(ref CachedRenderString cache, string fontName, float size, float maxWidth,
            string text,
            bool underline, TextAlignment alignment)
        {
            if (string.IsNullOrEmpty(text)) return Point.Zero;
            UpdateCache(ref cache, fontName, size, text, underline, alignment, maxWidth);
            return ((DirectWriteCachedText) cache).size;
        }
        public override void DrawStringCached(ref CachedRenderString cache, string fontName, float size, string text,
            float x, float y, Color4 color, bool underline = false, OptionalColor shadow = default,
            TextAlignment alignment = TextAlignment.Left, float maxWidth = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            UpdateCache(ref cache, fontName, size, text, underline, alignment, maxWidth);
            var pc = (DirectWriteCachedText) cache;
            if (shadow.Enabled)
            {
                foreach(var q in pc.quads) {
                    var d = q.Destination;
                    d.X += (int)x + 2;
                    d.Y += (int)y + 2;
                    if (q.Texture == null)
                        render2d.FillRectangle(d, shadow.Color);
                    else
                        render2d.Draw(q.Texture, q.Source, d, shadow.Color);
                }
            }
            foreach(var q in pc.quads) {
                var d = q.Destination;
                d.X += (int)x;
                d.Y += (int)y;
                if (q.Texture == null)
                    render2d.FillRectangle(d, color);
                else
                    render2d.Draw(q.Texture, q.Source, d, color);
            }
        }

        public override float LineHeight(string fontName, float size)
        {
            using (var layout = new TextLayout(dwFactory, "", GetFormat(fontName, ConvertSize(size)), float.MaxValue, float.MaxValue))
            {
                return layout.Metrics.Height;
            }
        }
        public override Point MeasureString(string fontName, float size, string text)
        {
            using (var layout = new TextLayout(dwFactory, text, GetFormat(fontName, ConvertSize(size)), float.MaxValue, float.MaxValue))
            {
                var metrics = layout.Metrics;
                return new Point((int)metrics.WidthIncludingTrailingWhitespace, (int)metrics.Height);
            }
        }
        public override void RenderText(BuiltRichText txt, int x, int y)
        {
            if (txt is EmptyRichText) return;
            var dw = (DirectWriteBuiltText)txt;
            foreach(var q in dw.Quads) {
                var d = q.Destination;
                d.X += x;
                d.Y += y;
                if (q.Texture == null)
                    render2d.FillRectangle(d, q.Color);
                else
                    render2d.Draw(q.Texture, q.Source, d, q.Color);
            }
        }
    }
    struct DrawQuad
    {
        public Texture2D Texture;
        public Rectangle Source;
        public Rectangle Destination;
        public Color4 Color;
    }
    class DirectWriteTextRenderer : TextRendererBase
    {
        const int MAX_GLYPH_SIZE = 384;
        const int TEXT_PAGE_SIZE = 1024;
        DWrite.BitmapRenderTarget renderTarget;
        RenderingParams renderParams;
        IntPtr hBrush;
        IntPtr hdc;
        IntPtr hbitmap;
        FontCollection fontCollection;
        List<Texture2D> pages = new List<Texture2D>();
        Dictionary<ulong, GlyphRect> glyphs = new Dictionary<ulong, GlyphRect>();
        int currentX = 0;
        int currentY = 0;
        int maxLineHeight = 0;
        int bytesPerPixel;
        IntPtr bmBits;
        DirectWriteText engine;
        private RenderContext rcontext;
        public DirectWriteTextRenderer(RenderContext context, DWrite.Factory dWriteFactory, DirectWriteText engine)
        {
            this.rcontext = context;
            renderTarget = dWriteFactory.GdiInterop.CreateBitmapRenderTarget(IntPtr.Zero, MAX_GLYPH_SIZE, MAX_GLYPH_SIZE);
            renderTarget.PixelsPerDip = 1f;
            hdc = renderTarget.MemoryDC;
            hBrush = GDI.CreateSolidBrush(0x00000000);
            hbitmap = GDI.GetCurrentObject(hdc, GDI.OBJ_BITMAP);
            GDI.DIBSECTION dib;
            GDI.GetObject(hbitmap, Marshal.SizeOf(typeof(GDI.DIBSECTION)), out dib);
            bytesPerPixel = dib.dsBm.bmBitsPixel / 8;
            bmBits = dib.dsBm.bmBits;
            fontCollection = dWriteFactory.GetSystemFontCollection(false);
            renderParams = new RenderingParams(dWriteFactory, 1.2f, 0, 0, PixelGeometry.Flat, RenderingMode.NaturalSymmetric);
            this.engine = engine;
            pages.Add(new Texture2D(context, TEXT_PAGE_SIZE, TEXT_PAGE_SIZE, false, SurfaceFormat.Bgra8));
        }

        struct ComputedSize
        {
            public int offsetX;
            public int offsetY;
            public int maxWidth;
            public int maxHeight;
            public ComputedSize(float fontSize, FontMetrics fontMetrics, GlyphMetrics glyphMetrics)
            {
                var fscale = fontSize / fontMetrics.DesignUnitsPerEm;
                var l = glyphMetrics.LeftSideBearing * fscale;
                var t = glyphMetrics.TopSideBearing * fscale;
                var r = glyphMetrics.RightSideBearing * fscale;
                var b = glyphMetrics.BottomSideBearing * fscale;
                var v = glyphMetrics.VerticalOriginY * fscale;
                var aw = glyphMetrics.AdvanceWidth * fscale;
                var ah = glyphMetrics.AdvanceHeight * fscale;

                offsetX = (int)Math.Floor(l);
                offsetY = (int)Math.Floor(t) - (int)Math.Floor(v);
                maxWidth = (int)(aw - r - l + 2.0f);
                maxHeight = (int)(ah - b - t + 2.0f);
            }
        }
        struct GlyphRect
        {
            public Texture2D Texture;
            public Rectangle Rectangle;
            public int OffsetX;
            public int OffsetY;
        }
        DWrite.Font GetFont(FontFace face)
        {
            DWrite.Font result;
            if (engine.customCollection.GetFontFromFontFace(face, out result))
                return result;
            return fontCollection.GetFontFromFontFace(face);
        }
        uint FontHash(FontFace face, float size)
        {
            var font = GetFont(face);
            var name = font.FontFamily.FamilyNames.GetString(0);
            var name2 = font.FaceNames.GetString(0);
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + name.GetHashCode();
                hash = hash * 31 + name2.GetHashCode();
                hash = hash * 31 + size.GetHashCode();
                return (uint)hash;
            }
        }
        static ulong GlyphHash(FontFace face, uint faceHash, short glyphIndex)
        {
            ushort simHash = 0;
            if ((face.Simulations & FontSimulations.Bold) == FontSimulations.Bold) simHash += 32;
            if ((face.Simulations & FontSimulations.Oblique) == FontSimulations.Oblique) simHash += 800;
            uint glyphHash = ((uint)glyphIndex << 16) | simHash;
            return (ulong)((ulong)faceHash << 32) | glyphHash;
        }
        unsafe GlyphRect GetGlyph(uint fontHash, FontFace fontFace, short glyphIndex, float fontSize)
        {
            var glyphHash = GlyphHash(fontFace, fontHash, glyphIndex);
            GlyphRect grect;
            if (!glyphs.TryGetValue(glyphHash, out grect))
            {
                var metrics = fontFace.Metrics;
                var glyphMetrics = fontFace.GetDesignGlyphMetrics(new short[] { glyphIndex }, false)[0];
                var run = new GlyphRun();
                run.FontFace = fontFace;
                run.FontSize = fontSize;
                run.Indices = new short[] { glyphIndex };
                run.Advances = new float[] { 0 };
                run.Offsets = new GlyphOffset[] { new GlyphOffset() };
                var gdata = new ComputedSize(fontSize, metrics, glyphMetrics);
                var fillRect = new GDI.RECT()
                {
                    left = 0,
                    top = 0,
                    right = 2 + gdata.maxWidth + 5,
                    bottom = 2 + gdata.maxHeight + 5
                };
                GDI.FillRect(hdc, ref fillRect, hBrush);
                RawRectangle rect;
                renderTarget.DrawGlyphRun(2.0f - gdata.offsetX, 2.0f - gdata.offsetY, D2D1.MeasuringMode.Natural, run, renderParams, new RawColorBGRA(255, 255, 255, 255), out rect);
                var left = rect.Left < 0 ? 0 : rect.Left;
                var right = rect.Right > MAX_GLYPH_SIZE ? MAX_GLYPH_SIZE : rect.Right;
                var top = rect.Top < 0 ? 0 : rect.Top;
                var bottom = rect.Bottom > MAX_GLYPH_SIZE ? MAX_GLYPH_SIZE : rect.Bottom;
                var r = new Rectangle(0, 0, right - left, bottom - top);
                //Construct grayscale image from GDI
                byte[] data = new byte[r.Width * r.Height * 4];
                byte* srcData = (byte*)bmBits;
                for (int y = 0; y < r.Height; y++)
                {
                    for (int x = 0; x < r.Width; x++)
                    {
                        var destP = (y * r.Width * 4) + (x * 4);
                        var pixel = srcData[(top + y) * bytesPerPixel * MAX_GLYPH_SIZE + (left + x) * bytesPerPixel];
                        data[destP] = data[destP + 1] = data[destP + 2] = 255;
                        data[destP + 3] = (byte)(Math.Pow(pixel / 255.0, 1.0 / 1.45) * 255.0);
                    }
                }
                //
                if (currentX + r.Width > MAX_GLYPH_SIZE)
                {
                    currentX = 0;
                    currentY += maxLineHeight;
                    maxLineHeight = 0;
                }
                if (currentY + r.Height > MAX_GLYPH_SIZE)
                {
                    pages.Add(new Texture2D(rcontext, TEXT_PAGE_SIZE, TEXT_PAGE_SIZE, false, SurfaceFormat.Bgra8));
                    currentX = currentY = maxLineHeight = 0;
                }
                r.X = currentX;
                r.Y = currentY;
                maxLineHeight = Math.Max(maxLineHeight, r.Height);
                currentX += r.Width;
                var page = pages[pages.Count - 1];
                page.SetData(0, r, data, 0, data.Length);
                grect.Texture = page;
                grect.OffsetX = (int)(gdata.offsetX + left - 2);
                grect.OffsetY = (int)(gdata.offsetY + top - 2);
                grect.Rectangle = r;
                glyphs.Add(glyphHash, grect);
            }
            return grect;
        }


        public List<DrawQuad> Quads = new List<DrawQuad>();
        public override Result DrawGlyphRun(
            object clientDrawingContext,
            float baselineOriginX,
            float baselineOriginY,
            D2D1.MeasuringMode measuringMode,
            GlyphRun glyphRun,
            GlyphRunDescription glyphRunDescription,
            ComObject clientDrawingEffect
            )
        {
            var fHash = FontHash(glyphRun.FontFace, glyphRun.FontSize);
            var positionX = (float)Math.Floor(baselineOriginX + 0.5f);
            var positionY = (float)Math.Floor(baselineOriginY + 0.5f);


            Color4 brushColor = Color4.White;
            OptionalColor shadow = new OptionalColor();
            OptionalColor background = new OptionalColor();
            if (clientDrawingEffect != null && clientDrawingEffect is ColorDrawingEffect colorFx)
            {
                brushColor = colorFx.Color;
                shadow = colorFx.Shadow;
                background = colorFx.Background;
            }

            if (background.Enabled)
            {
                float totalWidth = 0;
                for (int i = 0; i < glyphRun.Indices.Length; i++)
                    totalWidth += glyphRun.Advances[i];
                var metrics =  glyphRun.FontFace.Metrics;
                var adjust = glyphRun.FontSize / metrics.DesignUnitsPerEm;
                var ascent = adjust * metrics.Ascent;
                var descent = adjust * metrics.Descent;
                Quads.Add(new DrawQuad()
                {
                    Texture = null,
                    Color = background.Color,
                    Destination = new Rectangle(
                        (int)baselineOriginX,
                        (int)(baselineOriginY - ascent),
                        (int)totalWidth,
                        (int) (ascent + descent))
                });
            }

            for (int i = 0; i < glyphRun.Indices.Length; i++)
            {
                var glyph = GetGlyph(fHash, glyphRun.FontFace, glyphRun.Indices[i], glyphRun.FontSize);
                if (shadow.Enabled)
                {
                    Quads.Add(new DrawQuad()
                    {
                        Texture = glyph.Texture,
                        Source = glyph.Rectangle,
                        Destination = new Rectangle(
                            (int)(glyph.OffsetX + positionX + 2),
                            (int)(glyph.OffsetY + positionY + 2),
                            glyph.Rectangle.Width,
                            glyph.Rectangle.Height
                        ),
                        Color = shadow.Color
                    });
                }
                var q = new DrawQuad()
                {
                    Texture = glyph.Texture,
                    Source = glyph.Rectangle,
                    Destination = new Rectangle(
                        (int)(glyph.OffsetX + positionX),
                        (int)(glyph.OffsetY + positionY),
                        glyph.Rectangle.Width,
                        glyph.Rectangle.Height
                    ),
                    Color = brushColor
                };
                Quads.Add(q);
                positionX += glyphRun.Advances[i];
            }
            return Result.Ok;
        }
        public override Result DrawUnderline(
            object clientDrawingContext,
            float baselineOriginX,
            float baselineOriginY,
            ref Underline underline,
            ComObject clientDrawingEffect
            )
        {
            Color4 brushColor = Color4.White;
            OptionalColor shadow = new OptionalColor();
            if (clientDrawingEffect != null && clientDrawingEffect is ColorDrawingEffect colorFx) {
                brushColor = colorFx.Color;
                shadow = colorFx.Shadow;
            }
            if (shadow.Enabled)
            {
                Quads.Add(new DrawQuad()
                {
                    Texture = null,
                    Color = shadow.Color,
                    Destination = new Rectangle((int)baselineOriginX + 2, (int)(baselineOriginY + underline.Offset + 2), (int)Math.Ceiling(underline.Width), (int)Math.Ceiling(underline.Thickness))
                });
            }
            Quads.Add(new DrawQuad()
            {
                Texture = null,
                Color = brushColor,
                Destination = new Rectangle((int)baselineOriginX, (int)(baselineOriginY + underline.Offset), (int)Math.Ceiling(underline.Width), (int)Math.Ceiling(underline.Thickness))
            });
            return Result.Ok;
        }
    }
}
