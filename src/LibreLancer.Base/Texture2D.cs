// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreLancer
{
    public class Texture2D : Texture
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
		public bool WithAlpha = true;
		public bool Dxt1 = false;
        int glInternalFormat;
        int glFormat;
        int glType;

        public Texture2D(int width, int height, bool hasMipMaps, SurfaceFormat format) : this(true)
        {
            Width = width;
            Height = height;
            Format = format;
            Format.GetGLFormat(out glInternalFormat, out glFormat, out glType);
            LevelCount = hasMipMaps ? CalculateMipLevels(width, height) : 1;
            currentLevels = hasMipMaps ? (LevelCount - 1) : 0;
			//Bind the new TextureD
			GLBind.Trash();
			GLBind.BindTexture(4, GL.GL_TEXTURE_2D, ID);
			//initialise the texture data
			var imageSize = 0;
			Dxt1 = format == SurfaceFormat.Dxt1;
			if (glFormat == GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS)
            {
                CheckCompressed();
                switch (Format)
                {
                    case SurfaceFormat.Dxt1:
                    case SurfaceFormat.Dxt3:
                    case SurfaceFormat.Dxt5:
                        imageSize = ((Width + 3) / 4) * ((Height + 3) / 4) * format.GetSize();
                        break;
                    default:
                        throw new NotSupportedException();
                }
				GL.CompressedTexImage2D(GL.GL_TEXTURE_2D, 0, glInternalFormat,
                                        Width, Height, 0,
                                        imageSize, IntPtr.Zero);
            }
            else {
				GL.TexImage2D(GL.GL_TEXTURE_2D, 0,
                              glInternalFormat,
                              Width, Height, 0,
                              glFormat, glType, IntPtr.Zero);
            }
            //enable filtering
			GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
			GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
        }


        TextureFiltering currentFiltering = TextureFiltering.Linear;
        public void SetFiltering(TextureFiltering filtering)
		{
            if (currentFiltering == filtering) return;
            currentFiltering = filtering;
            BindTo(4);
            if (LevelCount > 1)
			{
                if (GLExtensions.Anisotropy && currentFiltering != TextureFiltering.Anisotropic) { 
                    GL.TexParameterf(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAX_ANISOTROPY_EXT, 1);
                }
                switch (filtering)
				{
                    case TextureFiltering.Anisotropic:
                        GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR_MIPMAP_LINEAR);
                        GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
                        if(GLExtensions.Anisotropy) {
                            GL.TexParameterf(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAX_ANISOTROPY_EXT, RenderState.Instance.AnisotropyLevel);
                        }
                        break;
					case TextureFiltering.Trilinear:
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR_MIPMAP_LINEAR);
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
						break;
					case TextureFiltering.Bilinear:
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR_MIPMAP_NEAREST);
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
						break;
					case TextureFiltering.Linear:
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
						break;
					case TextureFiltering.Nearest:
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_NEAREST);
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_NEAREST);
						break;
				}
			}
			else
			{
				switch (filtering)
				{
					case TextureFiltering.Nearest:
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_NEAREST);
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_NEAREST);
						break;
					default:
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
						GL.TexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
						break;
				}
			}

		}

        public Texture2D(int width, int height) : this(width, height, false, SurfaceFormat.Color)
        {

        }
        protected Texture2D(bool genID)
        {
			if (genID)
			{
				ID = GL.GenTexture();
			}
        }

        public override void BindTo(int unit)
        {
            GLBind.BindTexture(unit, GL.GL_TEXTURE_2D, ID);
            //Unit 4 is for creation, don't call it a trillion times
            if(unit != 4 && LevelCount > 1 && maxLevel != currentLevels) {
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
			BindTo(4);
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
		void GetMipSize(int level, out int width, out int height)
		{
			width = Width;
			height = Height;
			int i = 0;
			while (i < level) {
				width /= 2;
				height /= 2;
				i++;
			}
		}
		public unsafe void SetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T: struct
        {
            maxLevel = Math.Max(level, maxLevel);
            BindTo(4);
			if (glFormat == GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS)
            {
				int w, h;
				GetMipSize (level, out w, out h);
				var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
					GL.CompressedTexImage2D (GL.GL_TEXTURE_2D, level, glInternalFormat,
						w, h, 0,
						count, handle.AddrOfPinnedObject());
				handle.Free ();
            }
            else {
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
					var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
					GL.TexSubImage2D (GL.GL_TEXTURE_2D, level, x, y, w, h, glFormat, glType, handle.AddrOfPinnedObject());
					handle.Free ();
                }
                else {
                    w = Math.Max(Width >> level, 1);
                    h = Math.Max(Height >> level, 1);
					var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
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
			BindTo (4);
			GL.TexParameteri (GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_S, (int)mode);
		}
		public void SetWrapModeT(WrapMode mode)
		{
			if (mode == modeT)
				return;
			modeT = mode;
			BindTo (4);
			GL.TexParameteri (GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_T, (int)mode);
		}

		internal void SetData(int level, Rectangle rect, IntPtr data)
		{
			BindTo(4);
			GL.TexSubImage2D (GL.GL_TEXTURE_2D, 0, rect.X, rect.Y, rect.Width, rect.Height, glFormat, glType, data);
		}

        public void SetData<T>(T[] data) where T : struct
        {
            SetData<T>(0, null, data, 0, data.Length);
        }
    }
}

