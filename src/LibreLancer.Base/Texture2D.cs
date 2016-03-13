using System;
using System.IO;
using System.Drawing;
using Imaging = System.Drawing.Imaging;
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
        public static Texture2D FromStream(Stream stream)
        {
            //use a native library for this / roll own library?
            //system.drawing is a large dependency + we only use TGA and BMP
            Console.WriteLine("Texture2D.FromStream: Shouldn't be called!");
            Bitmap image = (Bitmap)Bitmap.FromStream(stream);
            try
            {
                // Fix up the Image to match the expected format
                image = (Bitmap)RGBToBGR(image);
                // TODO: make this more efficient. Shouldn't need another buffer
                var data = new byte[image.Width * image.Height * 4];
                Imaging.BitmapData bitmapData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    Imaging.ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppArgb);
                if (bitmapData.Stride != image.Width * 4)
                    throw new NotImplementedException();
                Marshal.Copy(bitmapData.Scan0, data, 0, data.Length);
                image.UnlockBits(bitmapData);
                Texture2D texture = null;
                texture = new Texture2D(image.Width, image.Height);
                texture.SetData(data);

                return texture;
            }
            finally
            {
                image.Dispose();
            }
        }
        private static float[][] matrixData = new float[][]
        {
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 1}
        };
        //Converts bitmaps to internal representation (includes .GIF support too!)
        internal static Image RGBToBGR(Image bmp)
        {
            Image result;
            if ((bmp.PixelFormat & Imaging.PixelFormat.Indexed) != 0)
                result = new Bitmap(bmp.Width, bmp.Height, Imaging.PixelFormat.Format32bppArgb);
            else
                result = bmp;
            try
            {
                var attributes = new Imaging.ImageAttributes();
                var colorMatrix = new Imaging.ColorMatrix(matrixData);
                attributes.SetColorMatrix(colorMatrix);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.DrawImage(bmp, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            finally
            {
                if (result != bmp)
                    bmp.Dispose();
            }
            return result;
        }

    }
}

