// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class MouseIni : IniFile
	{
		public string TxmFile;
		public string TextureName;
		public List<MouseShape> Shapes = new List<MouseShape>();
		public List<Cursor> Cursors = new List<Cursor>();
		public MouseIni(string filename, FileSystem vfs)
		{
			foreach (Section s in ParseFile(filename, vfs))
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
                        Cursors.Add(FromSection<Cursor>(s));
						break;
				}
			}
		}
	}

}
