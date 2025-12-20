// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Mouse
{
	public class MouseIni
	{
		public string TxmFile;
		public string TextureName;
		public List<MouseShape> Shapes = new List<MouseShape>();
		public List<Cursor> Cursors = new List<Cursor>();
		public MouseIni(string filename, FileSystem vfs, IniStringPool stringPool = null)
		{
			foreach (Section s in IniFile.ParseFile(filename, vfs, false, stringPool))
			{
				switch (s.Name.ToLowerInvariant())
				{
					case "texture":
						foreach (var e in s)
						{
							switch (e.Name.ToLowerInvariant())
							{
								case "file":
									TxmFile = e[0].ToString();
									break;
								case "name":
									TextureName = e[0].ToString();
									break;
							}
						}
						break;
					case "shape":
						Shapes.Add(new MouseShape(s));
						break;
					case "cursor":
                        if (Cursor.TryParse(s, out var cur))
                        {
                            Cursors.Add(cur);
                        }
						break;
				}
			}
		}
	}

}
