// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLTexture2D : GLTexture, ITexture2D
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool WithAlpha { get; set; } = true;
        public bool Dxt1 { get; set; } = false;

        int glInternalFormat;
        int glFormat;
        int glType;

        public GLTexture2D(int width, int height, bool hasMipMaps, SurfaceFormat format) : this(true)
        {
            Width = width;
            Height = height;
            Format = format;
            Format.GetGLFormat(out glInternalFormat, out glFormat, out glType);
            LevelCount = hasMipMaps ? CalculateMipLevels(width, height) : 1;
            currentLevels = hasMipMaps ? (LevelCount - 1) : 0;
			//Bind the new Texture2D
			GLBind.Trash();
            BindForModify();
			//initialise the texture data
			var imageSize = 0;
			Dxt1 = format == SurfaceFormat.Dxt1;
			if (glFormat == GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS)
            {
                if (GLExtensions.S3TC)
                {
                    switch (Format)
                    {
                        case SurfaceFormat.Dxt1:
                        case SurfaceFormat.Dxt3:
                        case SurfaceFormat.Dxt5:
                        case SurfaceFormat.Rgtc1:
                        case SurfaceFormat.Rgtc2:
                            imageSize = ((Width + 3) / 4) * ((Height + 3) / 4) * format.GetSize();
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    GL.CompressedTexImage2D(GL.GL_TEXTURE_2D, 0, glInternalFormat,
                        Width, Height, 0,
                        imageSize, IntPtr.Zero);
                }
                else
                {
                    imageSize = Width * Height * 4;
                    GL.TexImage2D(GL.GL_TEXTURE_2D, 0,
                        GL.GL_RGBA,
                        Width, Height, 0,
                        GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, IntPtr.Zero);
                }
            }
            else {
				GL.TexImage2D(GL.GL_TEXTURE_2D, 0,
                              glInternalFormat,
                              Width, Height, 0,
                              glFormat, glType, IntPtr.Zero);
            }
            EstimatedTextureMemory = imageSize == 0 ? Width * Height * format.GetSizeEstimate() : imageSize;
            if (hasMipMaps) {
                EstimatedTextureMemory = (int) (EstimatedTextureMemory * 1.33f);
            }
            //enable filtering
			GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
			GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
        }



        public void SetFiltering(TextureFiltering filtering)
		{
            if (currentFiltering == filtering) return;
            BindForModify();
            SetTargetFiltering(GL.GL_TEXTURE_2D, filtering);
        }

        protected GLTexture2D(bool genID)
        {
			if (genID)
			{
				ID = GL.GenTexture();
			}
        }

        void BindForModify()
            => GLBind.BindTextureForModify(GL.GL_TEXTURE_2D, ID);

        public override void BindTo(int unit)
        {
            if(IsDisposed) throw new ObjectDisposedException("Texture2D");
            if (unit == 4) throw new InvalidOperationException("Unit 4: Use BindForModify (private)");
            GLBind.BindTexture(unit, GL.GL_TEXTURE_2D, ID);
            if(LevelCount > 1 && maxLevel != currentLevels) {
                currentLevels = maxLevel;
                GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAX_LEVEL, maxLevel);
            }
        }

        int maxLevel = 0;
        int currentLevels = 0;

		//TODO: Re-implement Texture2D.GetData later
        public void GetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T : struct
        {
            GetData<T>(data);
        }
        public void GetData<T>(T[] data) where T : struct
        {
			BindForModify();
			if (glFormat == GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS)
            {
                throw new NotImplementedException();
            }
            else {
				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                GL.GetTexImage(
                    GL.GL_TEXTURE_2D,
                    0,
                    glFormat,
                    glType,
					handle.AddrOfPinnedObject()
                );
				handle.Free();
            }
        }

		public unsafe void SetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T: unmanaged
        {
            maxLevel = Math.Max(level, maxLevel);
            BindForModify();
			if (glFormat == GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS)
            {
				int w, h;
				GetMipSize (level, Width, Height, out w, out h);
				var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
					S3TC.CompressedTexImage2D (GL.GL_TEXTURE_2D, level, glInternalFormat,
						w, h, 0,
						count, handle.AddrOfPinnedObject());
				handle.Free ();
            }
            else
            {
                int w = Width;
                int h = Height;
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
					GL.TexSubImage2D (GL.GL_TEXTURE_2D, level, x, y, w, h, glFormat, glType, handle.AddrOfPinnedObject());
					handle.Free ();
                }
                else {
                    w = Math.Max(Width >> level, 1);
                    h = Math.Max(Height >> level, 1);
                    var conv = ConvertData(data, w, h);
                    GCHandle handle;
                    if(conv != null) handle = GCHandle.Alloc (conv, GCHandleType.Pinned);
                    else handle = GCHandle.Alloc (data, GCHandleType.Pinned);
					GL.TexImage2D (GL.GL_TEXTURE_2D, level, glInternalFormat, w, h, 0, glFormat, glType, handle.AddrOfPinnedObject());
					handle.Free ();
                }
            }
        }

		WrapMode modeS = 0;
		WrapMode modeT = 0;
		public void SetWrapModeS(WrapMode mode)
		{
			if (mode == modeS)
				return;
			modeS = mode;
            BindForModify();
			GL.TexParameteri (GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_S, (int)mode);
		}
		public void SetWrapModeT(WrapMode mode)
		{
			if (mode == modeT)
				return;
			modeT = mode;
            BindForModify();
			GL.TexParameteri (GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_T, (int)mode);
		}

		public void SetData(int level, Rectangle rect, IntPtr data)
		{
			BindForModify();
            if(Format == SurfaceFormat.R8)
                GL.PixelStorei(GL.GL_UNPACK_ALIGNMENT, 1);
			GL.TexSubImage2D (GL.GL_TEXTURE_2D, 0, rect.X, rect.Y, rect.Width, rect.Height, glFormat, glType, data);
            if(Format == SurfaceFormat.R8)
                GL.PixelStorei(GL.GL_UNPACK_ALIGNMENT, 4);
		}

        public void SetData<T>(T[] data) where T : unmanaged
        {
            SetData<T>(0, null, data, 0, data.Length);
        }
    }
}
