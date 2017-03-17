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
				return DDS.DDSFromStream2D (stream, 0, true);
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

