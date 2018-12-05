// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using StbSharp;
namespace LibreLancer.ImageLib
{
	public static class Generic
	{
		public static Texture2D FromFile(string file)
		{
			using(var stream = File.OpenRead(file)) {
				return FromStream (stream);
			}
		}
		public static Texture2D FromStream(Stream stream)
		{
			if (DDS.StreamIsDDS (stream)) {
                return (Texture2D)DDS.FromStream(stream);
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
				int x, y, comp;
				Stb.stbi_set_flip_vertically_on_load(1);
				var data = Stb.stbi_load_from_memory(b, out x, out y, out comp, Stb.STBI_rgb_alpha);
				unsafe
				{
					fixed(byte *d = data)
					{
						int j = 0;
						for (int i = 0; i < data.Length; i+=4)
						{
							var R = d[i];
							var G = d[i + 1];
							var B = d[i + 2];
							var A = d[i + 3];
							d[j++] = B;
							d[j++] = G;
							d[j++] = R;
							d[j++] = A;
						}
					}
				}
				var t = new Texture2D(x, y, false, SurfaceFormat.Color);
				t.SetData(data);
				return t;
			}
		}
	}
}

