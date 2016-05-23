/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace LibreLancer
{
    public class Texture2D : Texture
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        PixelInternalFormat glInternalFormat;
        PixelFormat glFormat;
        PixelType glType;

        public Texture2D(int width, int height, bool hasMipMaps, SurfaceFormat format) : this(true)
        {
            Width = width;
            Height = height;
            Format = format;
            Format.GetGLFormat(out glInternalFormat, out glFormat, out glType);
            LevelCount = hasMipMaps ? CalculateMipLevels(width, height) : 1;
            //Bind the new Texture2D
            Bind();
            //initialise the texture data
            var imageSize = 0;
            if (glFormat == (PixelFormat)All.CompressedTextureFormats)
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
                GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, glInternalFormat,
                                        Width, Height, 0,
                                        imageSize, IntPtr.Zero);
            }
            else {
                GL.TexImage2D(TextureTarget.Texture2D, 0,
                              glInternalFormat,
                              Width, Height, 0,
                              glFormat, glType, IntPtr.Zero);
            }
            //enable filtering
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        public Texture2D(int width, int height) : this(width, height, false, SurfaceFormat.Color)
        {

        }
        protected Texture2D(bool genID)
        {
            if (genID)
                ID = GL.GenTexture();
        }
        internal override void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, ID);
        }
        public void GetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T : struct
        {
            GetData<T>(data);
        }
        public void GetData<T>(T[] data) where T : struct
        {
            GL.BindTexture(TextureTarget.Texture2D, ID);
            if (glFormat == (PixelFormat)All.CompressedTextureFormats)
            {
                throw new NotImplementedException();
            }
            else {
                GL.GetTexImage<T>(
                    TextureTarget.Texture2D,
                    0,
                    glFormat,
                    glType,
                    data
                );
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
		public void SetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T: struct
        {
            GL.BindTexture(TextureTarget.Texture2D, ID);
            if (glFormat == (PixelFormat)All.CompressedTextureFormats)
            {
				int w, h;
				GetMipSize (level, out w, out h);
                GL.CompressedTexImage2D(TextureTarget.Texture2D, level, glInternalFormat,
                                         w, h, 0,
                                         count, data);
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
                    GL.TexSubImage2D(TextureTarget.Texture2D, level, x, y, w, h, glFormat, glType, data);
                }
                else {
                    w = Math.Max(Width >> level, 1);
                    h = Math.Max(Height >> level, 1);
                    GL.TexImage2D(TextureTarget.Texture2D, level, glInternalFormat, w, h, 0, glFormat, glType, data);
                }
            }
        }

		internal void SetData(int level, Rectangle rect, IntPtr data)
		{
			GL.TexSubImage2D (TextureTarget.Texture2D, 0, rect.X, rect.Y, rect.Width, rect.Height, glFormat, glType, data);
		}

        public void SetData<T>(T[] data) where T : struct
        {
            SetData<T>(0, null, data, 0, data.Length);
        }
        public override void Dispose()
        {
            GL.DeleteTexture(ID);
            base.Dispose();
        }
    }
}

