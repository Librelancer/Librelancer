// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using LibreLancer.Graphics;
using StbImageSharp;

namespace LibreLancer.ImageLib
{
    public static class Generic
    {
        public static Texture2D FromFile(RenderContext context, string file)
        {
            using(var stream = File.OpenRead(file)) {
                return (Texture2D)FromStream (context, stream);
            }
        }

        public struct LoadResult
        {
            public int Width;
            public int Height;
            public byte[] Data;
        }

        public static LoadResult BytesFromStream(Stream stream, bool flip = false)
        {
            int len = (int)stream.Length;
            byte[] b = new byte[len];
            int pos = 0;
            int r = 0;
            while ((r = stream.Read(b, pos, len - pos)) > 0)
            {
                pos += r;
            }
            /* stb_image it */
            StbImage.stbi_set_flip_vertically_on_load(flip ? 1 : 0);
            ImageResult image = ImageResult.FromMemory(b, ColorComponents.RedGreenBlueAlpha);
            return new LoadResult()
            {
                Width = image.Width, Height = image.Height, Data = image.Data
            };
        }

        public static unsafe Texture FromStream(RenderContext context, Stream stream, bool flip = true)
        {
            if (DDS.StreamIsDDS (stream)) {
                return DDS.FromStream(context, stream);
            } else {
                /* Read full stream */
                int len = (int)stream.Length;
                byte[] b = new byte[len];
                int pos = 0;
                int r = 0;
                while ((r = stream.Read(b, pos, len - pos)) > 0)
                {
                    pos += r;
                }
                /* stb_image it */
                int x = 0, y = 0;
                StbImage.stbi_set_flip_vertically_on_load(flip ? 1 : 0);
                ImageResult image = ImageResult.FromMemory(b, ColorComponents.RedGreenBlueAlpha);
                x = image.Width;
                y = image.Height;
                var data = image.Data;
                int dataLength = x * y * 4;
                int j = 0;
                for (int i = 0; i < dataLength; i+=4)
                {
                    var R = data[i];
                    var G = data[i + 1];
                    var B = data[i + 2];
                    var A = data[i + 3];
                    data[j++] = B;
                    data[j++] = G;
                    data[j++] = R;
                    data[j++] = A;
                }
                var t = new Texture2D(context, x, y, false, SurfaceFormat.Color);
                t.SetData(data);
                return t;
            }
        }
    }
}
