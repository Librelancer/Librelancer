// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreLancer.Graphics.Text.Pango
{
    class PangoBuiltText : BuiltRichText
    {
        internal IntPtr Handle;

        int height = 0;
        public override float Height => height;

        [DllImport("pangogame")]
        static extern void pg_destroytext(IntPtr text);
        [DllImport("pangogame")]
        static extern int pg_getheight(IntPtr text);
        [DllImport("pangogame")]
        static extern void pg_updatewidth(IntPtr text, int width);

        [DllImport("pangogame")]
        static extern void pg_get_caret_position(IntPtr text, int paragraph, int textPosition, out int outX, out int outY, out int outW, out int outH);

        private Dictionary<int, (int paragraph, int[] offsets)> offsetmap;
        internal PangoBuiltText(IntPtr ctx, IntPtr handle, Dictionary<int, (int paragraph, int[] offsets)> offsetmap)
        {
            Handle = handle;
            height = pg_getheight(handle);
            this.offsetmap = offsetmap;
        }

        private bool disposed = false;

        public override Rectangle GetCaretPosition(int layoutIndex, int textPosition)
        {
            var map = offsetmap[layoutIndex];
            pg_get_caret_position(Handle, map.paragraph, map.offsets[textPosition], out var x, out var y, out var w, out var h);
            return new Rectangle(x, y, w, h);
        }

        public override void Dispose()
        {
            disposed = true;
            pg_destroytext(Handle);
        }

        ~PangoBuiltText()
        {
            if (!disposed) Dispose();
        }

        int width = -1;
        public override void Recalculate(float width)
        {
            if (Math.Abs((int)width - this.width) < 2)
                return;
            this.width = (int)width;
            pg_updatewidth(Handle, (int)width);
            height = pg_getheight(Handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PGQuad
    {
        public PGTexture* Texture;
        public Rectangle Source;
        public Rectangle Dest;
        public Color4 Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PGTexture
    {
        public IntPtr UserData;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PGAttribute
    {
        public int StartIndex;
        public int EndIndex;
        public int Bold;
        public int Italic;
        public int Underline;
        public VertexDiffuse FgColor;
        public int FontSize;
        public IntPtr FontName;
        public int ShadowEnabled;
        public VertexDiffuse ShadowColor;
        public int BackgroundEnabled;
        public VertexDiffuse BackgroundColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PGParagraph
    {
        public IntPtr Text;
        public TextAlignment Alignment;
        public PGAttribute* Attributes;
        public int AttributeCount;
    }

    unsafe class HGlobalPool : IDisposable
    {
        private List<IntPtr> allocated = new List<IntPtr>();

        public IntPtr Allocate(string s)
        {
            var p = UnsafeHelpers.StringToHGlobalUTF8(s);
            allocated.Add(p);
            return p;
        }

        public T* Allocate<T>(int size) where T : unmanaged
        {
            var p = Marshal.AllocHGlobal(Marshal.SizeOf<T>() * size);
            allocated.Add(p);
            return (T*) p;
        }

        public void Dispose()
        {
            foreach(var p in allocated) Marshal.FreeHGlobal(p);
        }
    }


    unsafe class PangoText : RichTextEngine
    {
        [DllImport("pangogame")]
        static extern IntPtr pg_createcontext(IntPtr allocate, IntPtr update, IntPtr draw);
        [DllImport("pangogame")]
        public static extern IntPtr pg_buildtext(IntPtr ctx, IntPtr paragraphs, int paragraphCount, int width);
        [DllImport("pangogame")]
        static extern IntPtr pg_drawtext(IntPtr ctx, IntPtr text);

        [DllImport("pangogame")]
        static extern void pg_drawstring(IntPtr ctx, IntPtr str, IntPtr fontName, float fontSize, TextAlignment align,
            float maxWidth, int underline, float r, float g, float b, float a, Color4* shadow, float *oWidth, float *oHeight);

        [DllImport("pangogame")]
        static extern void pg_measurestring(IntPtr ctx, IntPtr str, IntPtr fontName, float fontSize, float maxWidth, out float width,
            out float height);

        [DllImport("pangogame")]
        static extern float pg_lineheight(IntPtr ctx, IntPtr fontName, float fontSize);

        delegate void PGDrawCallback(PGQuad* quads, int count);
        delegate void PGAllocateTextureCallback(PGTexture* texture, int width, int height, int isColor);
        delegate void PGUpdateTextureCallback(PGTexture* texture, IntPtr buffer, int x, int y, int width, int height);

        PGDrawCallback draw;
        PGAllocateTextureCallback alloc;
        PGUpdateTextureCallback update;
        private List<Texture2D> textures = new List<Texture2D>();
        Renderer2D ren;
        RenderContext context;
        IntPtr ctx;
        public PangoText(RenderContext context, Renderer2D renderer)
        {
            this.context = context;
            ren = renderer;
            draw = Draw;
            alloc = Alloc;
            update = Update;
            ctx = pg_createcontext(
                Marshal.GetFunctionPointerForDelegate(alloc),
                Marshal.GetFunctionPointerForDelegate(update),
                Marshal.GetFunctionPointerForDelegate(draw)
            );
        }

        void Draw(PGQuad* quads, int count)
        {
            if (drawX == int.MaxValue)
            {
                lastQuads = new PGQuad[count];
                for (int i = 0; i < lastQuads.Length; i++)
                    lastQuads[i] = quads[i];
                return;
            }
            for (int i = 0; i < count; i++)
            {
                var q = quads[i];
                q.Dest.X += drawX;
                q.Dest.Y += drawY;
                if (q.Texture == (PGTexture*) 0)
                {
                    ren.FillRectangle(q.Dest, q.Color);
                }
                else
                {
                    var t = textures[(int)q.Texture->UserData];
                    ren.Draw(t, q.Source, q.Dest, q.Color);
                }
            }
        }

        void Alloc(PGTexture *texture, int width, int height, int isColor)
        {
            textures.Add(new Texture2D(context, width, height, false, isColor == 0 ? SurfaceFormat.R8 : SurfaceFormat.Bgra8));
            texture->UserData = (IntPtr)(textures.Count - 1);
        }

        void Update(PGTexture *texture, IntPtr buffer, int x, int y, int width, int height)
        {
            var t = textures[(int)texture->UserData];
            var rect = new Rectangle(x, y, width, height);
            t.SetData(0, rect, buffer);
        }


        public override unsafe BuiltRichText BuildText(IList<RichTextNode> nodes, int width, float sizeMultiplier = 1f)
        {
            if(nodes.Count == 0)
                return new EmptyRichText();
            //Sort into paragraphs
            var paragraphs = new List<List<RichTextNode>>();
            paragraphs.Add(new List<RichTextNode>());
            int first = 0;
            while (nodes[first] is RichTextParagraphNode && first < nodes.Count) first++;
            var ta = (nodes[first] as RichTextTextNode).Alignment;
            paragraphs[paragraphs.Count - 1].Add(nodes[first]);
            foreach (var node in nodes.Skip(first + 1))
            {
                if (node is RichTextParagraphNode)
                    paragraphs.Add(new List<RichTextNode>());
                else
                {
                    var n = (RichTextTextNode)node;
                    var align = n.Alignment;
                    if (align != ta && paragraphs[paragraphs.Count - 1].Count > 0)
                        paragraphs.Add(new List<RichTextNode>());
                    paragraphs[paragraphs.Count - 1].Add(node);
                    ta = align;
                }
            }
            //Build markup
            using var pool = new HGlobalPool();
            PGParagraph[] native = new PGParagraph[paragraphs.Count];
            var offsetmap = new Dictionary<int, (int paragraph, int[] offsets)>();
            for(int i = 0; i < paragraphs.Count; i++)
            {
                PGAttribute* attrs = pool.Allocate<PGAttribute>(paragraphs[i].Count);
                native[i].AttributeCount = paragraphs[i].Count;
                native[i].Attributes = attrs;
                var builder = new StringBuilder();
                int idx = 0;
                TextAlignment a = TextAlignment.Left;
                for(int j = 0; j < paragraphs[i].Count; j++)
                {
                    var text = (RichTextTextNode)paragraphs[i][j];
                    a = text.Alignment;
                    attrs[j].StartIndex = idx;
                    int coffset = idx;
                    if (text.Contents.Length == 0)
                    {
                        offsetmap[nodes.IndexOf(paragraphs[i][j])] = (i, new int[] { coffset });
                    }
                    else
                    {
                        int[] offsets = new int[text.Contents.Length + 1];
                        Span<char> span = stackalloc char[1];
                        for (int k = 0; k < text.Contents.Length; k++)
                        {
                            offsets[k] = coffset;
                            span[0] = text.Contents[k];
                            coffset += Encoding.UTF8.GetByteCount(span);
                        }
                        offsets[^1] = coffset;
                        offsetmap[nodes.IndexOf(paragraphs[i][j])] = (i, offsets);
                    }

                    idx += Encoding.UTF8.GetByteCount(text.Contents);
                    attrs[j].EndIndex = idx;
                    builder.Append(text.Contents);
                    attrs[j].Bold = text.Bold ? 1 : 0;
                    attrs[j].Italic = text.Italic ? 1 : 0;
                    attrs[j].Underline = text.Underline ? 1 : 0;
                    if (text.Shadow.Enabled)
                    {
                        attrs[j].ShadowEnabled = 1;
                        attrs[j].ShadowColor = (VertexDiffuse)text.Shadow.Color;
                    }
                    else
                        attrs[j].ShadowEnabled = 0;
                    if (text.Background.Enabled)
                    {
                        attrs[j].BackgroundEnabled = 1;
                        attrs[j].BackgroundColor = (VertexDiffuse)text.Background.Color;
                    }
                    else
                        attrs[j].BackgroundEnabled = 0;
                    attrs[j].FgColor = (VertexDiffuse)text.Color;
                    attrs[j].FontName = pool.Allocate(text.FontName);
                    attrs[j].FontSize = (int)(text.FontSize * sizeMultiplier);
                }
                native[i].Text = pool.Allocate(builder.ToString());
                native[i].Alignment = a;
            }
            //Pass
            PangoBuiltText txt;
            fixed(PGParagraph* pPtr = native)
            {
                txt = new PangoBuiltText(ctx, pg_buildtext(ctx, (IntPtr)pPtr, native.Length, width), offsetmap);
            }
            return txt;
        }

        int drawX, drawY;
        public override void RenderText(BuiltRichText txt, int x, int y)
        {
            if (txt is EmptyRichText) return;
            drawX = x; drawY = y;
            pg_drawtext(ctx, ((PangoBuiltText)txt).Handle);
        }

        PGQuad[] lastQuads;
        // CACHES
        // TODO: Tune these
        struct StringResults
        {
            public int Hash;
            public float Size;
            public PGQuad[] Quads;
        }

        struct MeasureResults
        {
            public string Text;
            public int Font;
            public float Size;
            public Point Measured;
            public bool IsEqual(string text, int font, float size)
            {
                return ReferenceEquals(Text, text) &&
                       Math.Abs(Size - size) < 0.001f && Font == font;
            }
        }

        struct HeightResult
        {
            public int Hash;
            public float Size;
            public float LineHeight;
        }

        CircularBuffer<MeasureResults> measures = new CircularBuffer<MeasureResults>(64);
        CircularBuffer<StringResults> cachedStrings = new CircularBuffer<StringResults>(64);
        CircularBuffer<HeightResult> lineHeights = new CircularBuffer<HeightResult>(64);

        struct StringInfo
        {
            public int Underline;
            public unsafe int MakeHash(string fontName, string text)
            {
                fixed (StringInfo* si = &this)
                {
                    return FNV1A.Hash((IntPtr) si, sizeof(StringInfo),
                        FNV1A.Hash(text, FNV1A.Hash(fontName)));
                }
            }
        }


        public override void DrawStringBaseline(string fontName, float size, string text, float x, float y, Color4 color, bool underline = false, OptionalColor shadow = default)
        {
            if(string.IsNullOrEmpty(fontName)) throw new InvalidOperationException("fontName null");
            var pixels = size * (96.0f / 72.0f);
            drawX = int.MaxValue;
            drawY = int.MaxValue;
            StringInfo info = new StringInfo()
            {
                Underline = underline ? 1 : 0
            };
            var hash = info.MakeHash(fontName, text);
            PGQuad[] quads = null;
            for (int i = 0; i < cachedStrings.Count; i++) {
                if (cachedStrings[i].Hash == hash && Math.Abs(cachedStrings[i].Size - size) < 0.001f)
                {
                    quads = cachedStrings[i].Quads;
                    break;
                }
            }
            if (quads == null)
            {
                using var textConv = new UTF8ZHelper(stackalloc byte[256], text);
                using var fontConv = new UTF8ZHelper(stackalloc byte[256], fontName);
                fixed(byte *tC = &textConv.ToUTF8Z().GetPinnableReference(),
                      tF = &fontConv.ToUTF8Z().GetPinnableReference())
                {
                    pg_drawstring(ctx, (IntPtr)tC, (IntPtr)tF, pixels, TextAlignment.Left, 0, underline ? 1 : 0,  1, 1, 1, 1, (Color4*) 0,
                        (float*)0, (float*)0);
                }
                quads = lastQuads;
                lastQuads = null;
                cachedStrings.Enqueue(new StringResults() {Hash = hash, Size = size, Quads = quads});
            }

            drawX = (int) x;
            drawY = (int) y;
            if (shadow.Enabled)
            {
                for (int i = 0; i < quads.Length; i++)
                {
                    var q = quads[i];
                    q.Dest.X += drawX + 2;
                    q.Dest.Y += drawY + 2;
                    var t = textures[(int)q.Texture->UserData];
                    ren.Draw(t, q.Source, q.Dest, shadow.Color);
                }
            }
            for (int i = 0; i < quads.Length; i++)
            {
                var q = quads[i];
                q.Dest.X += drawX;
                q.Dest.Y += drawY;
                var t = textures[(int)q.Texture->UserData];
                ren.Draw(t, q.Source, q.Dest, color);
            }
        }


        public override Point MeasureString(string fontName, float size, string text)
        {
            if (string.IsNullOrEmpty(text)) return Point.Zero;
            if(string.IsNullOrEmpty(fontName)) throw new InvalidOperationException("fontName null");
            int fontHash = FNV1A.Hash(fontName);
            for (int i = 0; i < measures.Count; i++)
            {
                if (measures[i].IsEqual(text, fontHash, size))
                    return measures[i].Measured;
            }
            using var textConv = new UTF8ZHelper(stackalloc byte[256], text);
            using var fontConv = new UTF8ZHelper(stackalloc byte[256], fontName);
            fixed (byte* tC = &textConv.ToUTF8Z().GetPinnableReference(),
                tF = &fontConv.ToUTF8Z().GetPinnableReference())
            {
                pg_measurestring(ctx, (IntPtr)tC, (IntPtr)tF, size * (96.0f / 72.0f), 0, out var width, out var height);
                var p =  new Point((int)width, (int)height);
                measures.Enqueue(new MeasureResults() {Text = text, Font = fontHash, Size = size, Measured = p});
                return p;
            }
        }
        public override float LineHeight(string fontName, float size)
        {
            if(string.IsNullOrEmpty(fontName)) throw new InvalidOperationException("LineHeight fontName cannot be null");
            int fontHash = FNV1A.Hash(fontName);
            for (int i = 0; i < lineHeights.Count; i++)
            {
                if (lineHeights[i].Hash == fontHash && Math.Abs(lineHeights[i].Size - size) < 0.001f)
                    return lineHeights[i].LineHeight;
            }
            using var fontConv = new UTF8ZHelper(stackalloc byte[256], fontName);
            fixed (byte* tF = &fontConv.ToUTF8Z().GetPinnableReference())

            {
                var retval = pg_lineheight(ctx, (IntPtr)tF, size * (96.0f / 72.0f));
                lineHeights.Enqueue(new HeightResult() {Hash = fontHash, Size = size, LineHeight = retval});
                return retval;
            }
        }

        void UpdateCache(ref CachedRenderString cache, string fontName, float size, string text, bool underline,
            TextAlignment alignment, float maxWidth)
        {
            if (cache == null)
            {
                cache = new PangoRenderCache()
                {
                    FontName = fontName, FontSize = size, Text = text, Underline = underline,
                    Alignment = alignment, MaxWidth = maxWidth
                };
            }
            if (cache is not PangoRenderCache pc) throw new ArgumentException("cache");
            if (pc.quads == null || pc.Update(fontName, text, size, underline, alignment, maxWidth))
            {
                var pixels = size * (96.0f / 72.0f);
                drawX = int.MaxValue;
                drawY = int.MaxValue;
                using var textConv = new UTF8ZHelper(stackalloc byte[256], text);
                using var fontConv = new UTF8ZHelper(stackalloc byte[256], fontName);
                float szX, szY;
                fixed(byte *tC = &textConv.ToUTF8Z().GetPinnableReference(),
                    tF = &fontConv.ToUTF8Z().GetPinnableReference())
                {
                    pg_drawstring(ctx, (IntPtr)tC, (IntPtr)tF, pixels, alignment, maxWidth, underline ? 1 : 0, 1, 1, 1, 1, (Color4*) 0, &szX, &szY);
                }
                pc.quads = lastQuads;
                pc.size = new Point((int) szX, (int) szY);
                lastQuads = null;
            }
        }

        public override void DrawStringCached(ref CachedRenderString cache, string fontName, float size, string text, float x, float y,
            Color4 color, bool underline = false, OptionalColor shadow = default, TextAlignment alignment = TextAlignment.Left, float maxWidth = 0)
        {
            UpdateCache(ref cache, fontName, size, text, underline, alignment, maxWidth);
            var pc = (PangoRenderCache) cache;
            drawX = (int) x;
            drawY = (int) y;
            if (shadow.Enabled)
            {
                for (int i = 0; i < pc.quads.Length; i++)
                {
                    var q = pc.quads[i];
                    q.Dest.X += drawX + 2;
                    q.Dest.Y += drawY + 2;
                    var t = textures[(int)q.Texture->UserData];
                    ren.Draw(t, q.Source, q.Dest, shadow.Color);
                }
            }
            for (int i = 0; i < pc.quads.Length; i++)
            {
                var q = pc.quads[i];
                q.Dest.X += drawX;
                q.Dest.Y += drawY;
                var t = textures[(int)q.Texture->UserData];
                ren.Draw(t, q.Source, q.Dest, color);
            }
        }

        public override Point MeasureStringCached(ref CachedRenderString cache, string fontName, float size, float maxWidth, string text, bool underline,
            TextAlignment alignment)
        {
            UpdateCache(ref cache, fontName, size, text, underline, alignment, maxWidth);
            var pc = (PangoRenderCache) cache;
            return pc.size;
        }

        class PangoRenderCache : CachedRenderString
        {
            internal PGQuad[] quads;
            internal Point size;
        }

        public override void Dispose()
        {
        }
    }
}
