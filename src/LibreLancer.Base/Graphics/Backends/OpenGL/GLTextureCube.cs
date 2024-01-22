// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics
{
    class GLTextureCube : GLTexture, ITextureCube
    {
        public int Size { get; private set; }

        int glInternalFormat;
        int glFormat;
        int glType;

        public GLTextureCube (int size, bool mipMap, SurfaceFormat format)
        {
            ID = GL.GenTexture();
            Size = size;
            Format = format;
            Format.GetGLFormat(out glInternalFormat, out glFormat, out glType);
            LevelCount = mipMap ? CalculateMipLevels(size, size) : 1;

            //Bind the new TextureCube
            BindForModify();
            //enable filtering
            GL.TexParameteri(GL.GL_TEXTURE_CUBE_MAP, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
            GL.TexParameteri(GL.GL_TEXTURE_CUBE_MAP, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
            //initialise
            if (glFormat == GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS)
            {
                int imageSize = 0;
                if (GLExtensions.S3TC)
                {
                    switch (Format)
                    {
                        case SurfaceFormat.Dxt1:
                        case SurfaceFormat.Dxt3:
                        case SurfaceFormat.Dxt5:
                            imageSize = ((size + 3) / 4) * ((size + 3) / 4) * format.GetSize();
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    var target = ((CubeMapFace)i).ToGL();
                    if (GLExtensions.S3TC)
                    {
                        GL.CompressedTexImage2D(target, 0, glInternalFormat,
                            size, size, 0,
                            imageSize, IntPtr.Zero);
                    }
                    else
                    {
                        GL.TexImage2D(target, 0,
                            GL.GL_RGBA,
                            size, size, 0,
                            GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, IntPtr.Zero);
                    }

                }
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    var target = ((CubeMapFace)i).ToGL();
                    GL.TexImage2D(target, 0, glInternalFormat,
                        size, size, 0, glFormat, glType, IntPtr.Zero);
                }
            }
        }

        private int maxLevel = 0;
        private int currentLevels = 0;
        public void SetData<T>(CubeMapFace face, int level, Rectangle? rect, T[] data, int start, int count) where T : unmanaged
        {
            int target = face.ToGL();
            maxLevel = Math.Max(level, maxLevel);
            BindForModify();
            if (glFormat == GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS)
            {
                int w, h;
                GetMipSize (level, Size, Size, out w, out h);
                var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
                S3TC.CompressedTexImage2D (target, level, glInternalFormat,
                    w, h, 0,
                    count, handle.AddrOfPinnedObject());
                handle.Free ();
            }
            else {
                int w = Size;
                int h = Size;
                int x = 0;
                int y = 0;
                if (rect.HasValue)
                {
                    w = rect.Value.Width;
                    h = rect.Value.Height;
                    x = rect.Value.X;
                    y = rect.Value.Y;
                    var conv = ConvertData(data, w, h);
                    GCHandle handle;
                    if(conv != null) handle = GCHandle.Alloc (conv, GCHandleType.Pinned);
                    else handle = GCHandle.Alloc (data, GCHandleType.Pinned);
                    GL.TexSubImage2D (target, level, x, y, w, h, glFormat, glType, handle.AddrOfPinnedObject());
                    handle.Free ();
                }
                else {
                    w = Math.Max(Size >> level, 1);
                    h = Math.Max(Size >> level, 1);
                    var conv = ConvertData(data, w, h);
                    GCHandle handle;
                    if(conv != null) handle = GCHandle.Alloc (conv, GCHandleType.Pinned);
                    else handle = GCHandle.Alloc (data, GCHandleType.Pinned);
                    GL.TexImage2D (target, level, glInternalFormat, w, h, 0, glFormat, glType, handle.AddrOfPinnedObject());
                    handle.Free ();
                }
            }
        }

        public void SetData<T>(CubeMapFace face, T[] data) where T : unmanaged
        {
            SetData<T>(face, 0, null, data, 0, data.Length);
        }

        public void SetFiltering(TextureFiltering filtering)
        {
            if (currentFiltering == filtering) return;
            BindForModify();
            SetTargetFiltering(GL.GL_TEXTURE_CUBE_MAP, filtering);
        }

        void BindForModify()
            => GLBind.BindTextureForModify(GL.GL_TEXTURE_CUBE_MAP, ID);

        public override void BindTo(int unit)
        {
            if(IsDisposed) throw new ObjectDisposedException("TextureCube");
            if (unit == 4) throw new InvalidOperationException("Unit 4: Use BindForModify (private)");
            GLBind.BindTexture(unit, GL.GL_TEXTURE_CUBE_MAP, ID);
            if(LevelCount > 1 && maxLevel != currentLevels) {
                currentLevels = maxLevel;
                GL.TexParameteri(GL.GL_TEXTURE_CUBE_MAP, GL.GL_TEXTURE_MAX_LEVEL, maxLevel);
            }
        }
    }
}
