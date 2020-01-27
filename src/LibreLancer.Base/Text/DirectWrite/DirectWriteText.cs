// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using LibreLancer.Platforms.Win32;

using DWrite = SharpDX.DirectWrite;
using D2D1 = SharpDX.Direct2D1;

namespace LibreLancer.Text.DirectWrite
{
    class ColorDrawingEffect : ComObject
    {
        public Color4 Color { get; set; }
        public ColorDrawingEffect(Color4 color)
        {
            Color = color;
        }
    }

    class DirectWriteBuiltText : BuiltRichText
    {
        float height = 1f;
        public List<TextLayout> Layout;
        public int Width;

        public override float Height => height;

        public override void Dispose()
        {
            foreach (var l in Layout) l.Dispose();
        }

        public override void Recalculate(float width)
        {
            if (((int)width) == Width) return;
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

    }
    class DirectWriteText : RichTextEngine
    {
        Factory dwFactory;
        public DirectWriteTextRenderer Renderer;
        public FontCollection customCollection;
        CustomFontLoader customLoader;
        Renderer2D render2d;
        public DirectWriteText(Renderer2D r2d)
        {
            dwFactory = new Factory();
            Renderer = new DirectWriteTextRenderer(dwFactory, this);
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
                    layout.SetDrawingEffect(new ColorDrawingEffect(text.Color), range);
                    startIdx += text.Contents.Length;
                }
                layouts.Add(layout);
            }
            //Return
            var built = new DirectWriteBuiltText(this) { Layout = layouts, Width = width };
            built.CacheQuads();
            return built;
        }

        public override void Dispose()
        {

        }

        public override void RenderText(BuiltRichText txt, int x, int y)
        {
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
        public DirectWriteTextRenderer(DWrite.Factory dWriteFactory, DirectWriteText engine)
        {
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
            pages.Add(new Texture2D(TEXT_PAGE_SIZE, TEXT_PAGE_SIZE, false, SurfaceFormat.Color));
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
                    pages.Add(new Texture2D(TEXT_PAGE_SIZE, TEXT_PAGE_SIZE, false, SurfaceFormat.Color));
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
            if (clientDrawingEffect != null && clientDrawingEffect is ColorDrawingEffect)
                brushColor = (clientDrawingEffect as ColorDrawingEffect).Color;
            for (int i = 0; i < glyphRun.Indices.Length; i++)
            {
                var glyph = GetGlyph(fHash, glyphRun.FontFace, glyphRun.Indices[i], glyphRun.FontSize);
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
            if (clientDrawingEffect != null && clientDrawingEffect is ColorDrawingEffect)
                brushColor = (clientDrawingEffect as ColorDrawingEffect).Color;
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
