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
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.GameData.Universe
{
	public class TexturePanels : IniFile
	{
		public string File { get; private set; }
		public string TextureShape { get; private set; }
		public List<TextureShape> Shapes { get; private set; }

		public TexturePanels(string filename)
		{
			var parsed = ParseFile (filename);
			if (parsed.Count != 1)
				throw new Exception ("Shape ini must have ONE section");
			if (parsed [0].Name != "Texture")
				throw new Exception (string.Format ("Expected [Texture], got [{0}]", parsed [0].Name));
			Init (parsed [0]);
		}

		void Init(Section section)
		{
			Shapes = new List<TextureShape> ();
			string current_texname = null;
			for (int i = 0; i < section.Count; i++)
			{
				Entry e = section [i];
				switch (e.Name.ToLowerInvariant())
				{
				case "file":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (File != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					File = e[0].ToString();
					break;
				case "texture_name":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					current_texname = e [0].ToString ();
					break;
				case "tex_shape":
					//TODO: I have no idea what this value does - Callum
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TextureShape != null)
						throw new Exception ("Already have tex_shape entry");
					TextureShape = e [0].ToString ();
					break;
				case "shape_name":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					var shape_name = e [0].ToString ();
					if (i + 1 >= section.Count)
						throw new Exception ("shape_name needs accompanying dim");
					e = section [i + 1];
					if (e.Name != "dim")
						throw new Exception ("expected dim, got " + e.Name);
					if (e.Count != 4)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					Shapes.Add(new TextureShape(
						current_texname,
						shape_name,
						new RectangleF(
							e[0].ToSingle(), 
							e[1].ToSingle(), 
							e[2].ToSingle(), 
							e[3].ToSingle()
						)
					));
					i++;
					break;
				default: throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}
		}

		public override string ToString()
		{
			return File;
		}
	}
}