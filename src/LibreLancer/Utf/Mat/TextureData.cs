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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.IO;
using OpenTK;


namespace LibreLancer.Utf.Mat
{
	public class TextureData
	{
		private string type;
		private string texname;
		private byte[] data;

		public Texture Texture { get; private set; }

		public TextureData (LeafNode node, string texname)
		{
			this.type = node.Name;
			this.texname = texname;
			this.data = node.ByteArrayData;
		}

		public void Initialize ()
		{
			if (data != null) {
				using (Stream stream = new MemoryStream (data)) {
					if (type.Equals ("mips", StringComparison.OrdinalIgnoreCase)) {
						Texture = DDSLib.DDSFromStream2D (stream, 0, true);
					} else if (type.StartsWith ("mip", StringComparison.OrdinalIgnoreCase)) {
						var tex = TGALib.TGAFromStream (stream);
						if (tex != null)
							Texture = tex;
					} else if (type.Equals ("cube", StringComparison.OrdinalIgnoreCase)) {
						Texture = DDSLib.DDSFromStreamCube (stream, 0, true);
					}
				}
			} else
				FLLog.Error ("Texture " + texname, "data == null");
		}

		public override string ToString ()
		{
			return type;
		}
	}
}
