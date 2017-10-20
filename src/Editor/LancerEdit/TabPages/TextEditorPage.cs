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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using System.Text;
using System.Linq;
using XwtPlus.TextEditor;
namespace LancerEdit
{
	public class TextEditorPage : LTabPage
	{
		TextEditor editor;
		bool wasbini;
		public TextEditorPage(string name) : base (name)
		{
			editor = new TextEditor();
			PackStart(editor, true, true);
		}

		public void Load(string filename)
		{
			editor.Document.Text = IniParse.LoadFile(filename, out wasbini);
			if (wasbini) TabName += " (" + Txt._("Compressed") + ")";
		}
	}

	class IniParse : LibreLancer.Ini.IniFile
	{
		public static string LoadFile(string path, out bool wasbini)
		{
			bool bini;
			using (var reader = new BinaryReader(File.OpenRead(path)))
			{
				var str = Encoding.ASCII.GetString(reader.ReadBytes(4));
				bini = str == "BINI";
			}
			wasbini = bini;
			if (!bini)
				return File.ReadAllText(path);
			var parse = new IniParse();
			var builder = new StringBuilder();

			foreach (var section in parse.ParseFile(path))
			{
				builder.Append('[').Append(section.Name).AppendLine("]");
				foreach (var entry in section)
				{
					builder.Append(entry).Append('=');
					builder.AppendLine(string.Join(",", entry.Select((v) => v.ToString())));
				}
			}
			return builder.ToString();
		}
	}
}
