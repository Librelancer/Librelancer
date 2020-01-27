// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibreLancer.Text.Pango
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
        internal PangoBuiltText(IntPtr ctx, IntPtr handle)
        {
            Handle = handle;
            height = pg_getheight(handle);
        }

        public override void Dispose()
        {
            pg_destroytext(Handle);
        }

        int width = -1;
        public override void Recalculate(float width)
        {
            if ((int)width == this.width)
                return;
            this.width = (int)width;
            pg_updatewidth(Handle, (int)width);
            height = pg_getheight(Handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PGQuad
    {
        public Rectangle Source;
        public Rectangle Dest;
        public Color4 Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PGTexture
    {
        public IntPtr UserData;
    }

    unsafe class PangoText : RichTextEngine
    {
        [DllImport("pangogame")]
        static extern IntPtr pg_createcontext(IntPtr allocate, IntPtr update, IntPtr draw);
        [DllImport("pangogame")]
        public static extern IntPtr pg_buildtext(IntPtr ctx, IntPtr markups, IntPtr alignments, int count, int width);
        [DllImport("pangogame")]
        static extern IntPtr pg_drawtext(IntPtr ctx, IntPtr text);

        delegate void PGDrawCallback(PGQuad* quads, PGTexture* texture, int count);
        delegate void PGAllocateTextureCallback(PGTexture* texture, int width, int height);
        delegate void PGUpdateTextureCallback(PGTexture* texture, IntPtr buffer, int x, int y, int width, int height);

        PGDrawCallback draw;
        PGAllocateTextureCallback alloc;
        PGUpdateTextureCallback update;
        List<Texture2D> textures = new List<Texture2D>();


        Renderer2D ren;
        IntPtr ctx;
        public PangoText(Renderer2D renderer)
        {
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

        void Draw(PGQuad* quads, PGTexture *texture, int count)
        {
            if (texture == (PGTexture*)0)
            {
                for (int i = 0; i < count; i++)
                {
                    var q = quads[i];
                    q.Dest.X += drawX;
                    q.Dest.Y += drawY;
                    ren.FillRectangle(q.Dest, q.Color);
                }
            }
            else
            {
                var t = textures[(int)texture->UserData];
                for (int i = 0; i < count; i++)
                {
                    var q = quads[i];
                    q.Dest.X += drawX;
                    q.Dest.Y += drawY;
                    ren.Draw(t, q.Source, q.Dest, q.Color);
                }
            }
        }

        void Alloc(PGTexture *texture, int width, int height)
        {
            textures.Add(new Texture2D(width, height, false, SurfaceFormat.R8));
            texture->UserData = (IntPtr)(textures.Count - 1);
        }

        void Update(PGTexture *texture, IntPtr buffer, int x, int y, int width, int height)
        {
            var t = textures[(int)texture->UserData];
            GL.PixelStorei(GL.GL_UNPACK_ALIGNMENT, 1);
            var rect = new Rectangle(x, y, width, height);
            t.SetData(0, rect, buffer);
            GL.PixelStorei(GL.GL_UNPACK_ALIGNMENT, 4);
        }

        
        public override unsafe BuiltRichText BuildText(IList<RichTextNode> nodes, int width, float sizeMultiplier = 1f)
        {
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
            var markups = new List<string>();
            var alignments = new List<TextAlignment>();
            for(int i = 0; i < paragraphs.Count; i++)
            {
                //make span
                var builder = new StringBuilder();
                TextAlignment a = TextAlignment.Left;
                foreach (var tn in paragraphs[i])
                {
                    var text = (RichTextTextNode)tn;
                    a = text.Alignment;
                    builder.Append("<span ");
                    if (text.Italic)
                        builder.Append("font_style=\"italic\" ");
                    else
                        builder.Append("font_style=\"normal\" ");
                    if (text.Bold)
                        builder.Append("font_weight=\"bold\" ");
                    else
                        builder.Append("font_weight=\"normal\" ");
                    builder.Append("fgcolor=\"#");
                    builder.Append(((int)(text.Color.R * 255f)).ToString("X2"));
                    builder.Append(((int)(text.Color.G * 255f)).ToString("X2"));
                    builder.Append(((int)(text.Color.B * 255f)).ToString("X2"));
                    builder.Append("\" underline=\"");
                    if (text.Underline) builder.Append("single\" ");
                    else builder.Append("none\" ");
                    builder.Append("size=\"");
                    builder.Append((int)(text.FontSize * sizeMultiplier * 1024));
                    builder.Append("\" font_family=\"");
                    builder.Append(text.FontName);
                    builder.Append("\">");
                    builder.Append(System.Net.WebUtility.HtmlEncode(text.Contents));
                    builder.Append("</span>");
                }
                markups.Add(builder.ToString());
                alignments.Add(a);
            }
            //Pass
            IntPtr[] stringPointers = new IntPtr[markups.Count];
            for(int i = 0; i < markups.Count; i++)
            {
                stringPointers[i] = UnsafeHelpers.StringToHGlobalUTF8(markups[i]);
            }
            var aligns = alignments.ToArray();
            PangoBuiltText txt;
            fixed(IntPtr* stringPtr = stringPointers)
            {
                fixed(TextAlignment *alignPtr = aligns)
                {
                    txt = new PangoBuiltText(ctx, pg_buildtext(ctx, (IntPtr)stringPtr, (IntPtr)alignPtr, markups.Count, width));
                }
            }
            for (int i = 0; i < markups.Count; i++)
            {
                Marshal.FreeHGlobal(stringPointers[i]);
            }
            return txt;
        }

        int drawX, drawY;
        public override void RenderText(BuiltRichText txt, int x, int y)
        {
            drawX = x; drawY = y;
            pg_drawtext(ctx, ((PangoBuiltText)txt).Handle);
        }

        public override void Dispose()
        {
        }
    }
}
