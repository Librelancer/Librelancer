// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace LibreLancer.Text.Pango
{
    class PangoBuiltText : BuiltRichText
    {
        internal IntPtr Handle;
        [DllImport("pangogame")]
        static extern void pg_destroytext(IntPtr text);
        internal PangoBuiltText(IntPtr h)
        {
            Handle = h;
        }
        public override void Dispose()
        {
            pg_destroytext(Handle);
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
        static extern IntPtr pg_buildtext(IntPtr ctx, IntPtr markup, int width);
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
            var t = textures[(int)texture->UserData];
            for(int i = 0; i < count; i++)
            {
                var q = quads[i];
                q.Source.X += drawX;
                q.Source.Y += drawY;
                ren.Draw(t, q.Source, q.Dest, q.Color);
            }
        }

        void Alloc(PGTexture *texture, int width, int height)
        {
            textures.Add(new Texture2D(width, height, false, SurfaceFormat.R8));
            texture->UserData = (IntPtr)(textures.Count - 1);
            Console.WriteLine("alloced {0}", (int)texture->UserData);
        }

        void Update(PGTexture *texture, IntPtr buffer, int x, int y, int width, int height)
        {
            var t = textures[(int)texture->UserData];
            GL.PixelStorei(GL.GL_UNPACK_ALIGNMENT, 1);
            var rect = new Rectangle(x, y, width, height);
            t.SetData(0, rect, buffer);
            GL.PixelStorei(GL.GL_UNPACK_ALIGNMENT, 4);
        }

        public override BuiltRichText BuildText(string markup, int width)
        {
            var ptr = UnsafeHelpers.StringToHGlobalUTF8(markup);
            var txt = new PangoBuiltText(pg_buildtext(ctx, ptr, width));
            Marshal.FreeHGlobal(ptr);
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
