// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer
{
    public class TextureCube : Texture
    {
        public int Size { get; private set; }

        int glInternalFormat;
        int glFormat;
        int glType;

        public TextureCube( int size, bool mipMap, SurfaceFormat format)
        {
            ID = GL.GenTexture();
            Size = size;
            Format = format;
            Format.GetGLFormat(out glInternalFormat, out glFormat, out glType);
            LevelCount = mipMap ? CalculateMipLevels(size, size) : 1;
			if (glFormat == GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS)
                throw new NotImplementedException("Compressed cubemaps");
            //Bind the new TextureCube
            BindTo(0);
            //enable filtering
			GL.TexParameteri(GL.GL_TEXTURE_CUBE_MAP, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
			GL.TexParameteri(GL.GL_TEXTURE_CUBE_MAP, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
            //initialise
            for (int i = 0; i < 6; i++)
            {
                var target = ((CubeMapFace)i).ToGL();
                GL.TexImage2D(target, 0, glInternalFormat,
                    size, size, 0, glFormat, glType, IntPtr.Zero);
            }
            if (mipMap)
            {
				//This isn't actually supported on GL 3, why is it here?
				//GL.TexParameteri(GL.GL_TEXTURE_CUBE_MAP, TextureParameterName.GenerateMipmap, 1);
            }
        }

        public void SetData<T>(CubeMapFace face, int level, Rectangle? rect, T[] data, int start, int count) where T : struct
        {
            int x, y, w, h;
            if (rect.HasValue)
            {
                x = rect.Value.X;
                y = rect.Value.Y;
                w = rect.Value.Width;
                h = rect.Value.Height;
            }
            else {
                x = 0;
                y = 0;
                w = Math.Max(1, Size >> level);
                h = Math.Max(1, Size >> level);
            }
			GL.BindTexture(GL.GL_TEXTURE_CUBE_MAP, ID);
			var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
			GL.TexSubImage2D (face.ToGL (), level, x, y, w, h, glFormat, glType, handle.AddrOfPinnedObject());
			handle.Free ();
        }

        public void SetData<T>(CubeMapFace face, T[] data) where T : struct
        {
            SetData<T>(face, 0, null, data, 0, data.Length);
        }

        public override void BindTo(int unit)
        {
			GLBind.BindTexture(unit, GL.GL_TEXTURE_CUBE_MAP, ID);
        }
    }
}

