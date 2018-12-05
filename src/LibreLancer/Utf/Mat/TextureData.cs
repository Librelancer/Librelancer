// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf.Mat
{
	public class TextureData
	{
        public static bool Bitch = false;

		private string type;
		private string texname;
		private byte[] data;
		public Texture Texture { get; private set; }
		Dictionary<int, byte[]> levels;

		public TextureData (LeafNode node, string texname, bool isTgaMips)
		{
			this.type = node.Name;
			this.texname = texname;
			this.data = node.ByteArrayData;
			if (isTgaMips)
				levels = new Dictionary<int, byte[]>();
		}

		public TextureData(string filename)
		{
			Texture = ImageLib.Generic.FromFile(filename);
		}

		public void Initialize ()
		{
			if (data != null && Texture == null) {
				using (Stream stream = new MemoryStream (data)) {
					if (type.Equals ("mips", StringComparison.OrdinalIgnoreCase)) {
                        Texture = ImageLib.DDS.FromStream(stream);
					} else if (type.StartsWith ("mip", StringComparison.OrdinalIgnoreCase)) {;
						var tga = ImageLib.TGA.FromStream(stream, levels != null);
                        if(tga == null) {
                            FLLog.Error("Mat","Texture " + texname + "\\MIP0" + " is bad");
                            if (Bitch) throw new Exception("Your texture data is bad, fix it!\n" +
                                                           texname + "\\MIP0 to be exact");
                            Texture = null;
                            return;
                        }
						if (levels != null)
						{
							foreach (var lv in levels)
							{
								using (var s2 = new MemoryStream(lv.Value)) {
									ImageLib.TGA.FromStream(s2, true, tga, lv.Key);
								}
							}
						}
						Texture = tga;
						levels = null;
					} else if (type.Equals ("cube", StringComparison.OrdinalIgnoreCase)) {
						Texture = ImageLib.DDS.FromStream (stream);
					}
				}
			} else
				FLLog.Error ("Texture " + texname, "data == null");
		}

		public void SetLevel(Node node)
		{
			var n = node as LeafNode;
			if (n == null)
				throw new Exception("Invalid node in TextureData MIPS " + node.Name);
			var name = n.Name.Trim();
			if (!name.StartsWith("mip", StringComparison.OrdinalIgnoreCase))
				throw new Exception("Invalid node in TextureData MIPS " + node.Name);
			var mipLevel = int.Parse(name.Substring(3));
			if (mipLevel == 0)
				return;
			levels.Add(mipLevel, n.ByteArrayData);
		}

		public override string ToString ()
		{
			return type;
		}
	}
}
