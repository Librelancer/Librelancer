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
 * Data structure by Bas Westerbaan (http://blog.w-nz.com/uploads/bini.pdf)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.Compatibility;

namespace LibreLancer.Ini
{
	//[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public abstract class IniFile //: ICollection<Section>
	{
		public const string FileType = "BINI", IniFileType = "INI";
		public const int FileVersion = 1;

		protected List<Section> ParseFile(string path, bool allowmaps = false)
		{
			if (path == null) throw new ArgumentNullException("path");

			List<Section> sections = new List<Section>();

			using (Stream stream = VFS.Open(path))
			{
				byte[] buffer = new byte[4];
				stream.Read(buffer, 0, 4);
				string fileType = Encoding.ASCII.GetString(buffer);

				if (fileType == FileType) // Binary Ini
				{
					BinaryReader reader = new BinaryReader(stream);

					int formatVersion = reader.ReadInt32();
					if (formatVersion != FileVersion) throw new FileVersionException(path, fileType, formatVersion, FileVersion);

					int stringBlockOffset = reader.ReadInt32();
					if (stringBlockOffset > reader.BaseStream.Length) throw new FileContentException(path, fileType, "The string block offset was out of range: " + stringBlockOffset);

					long sectionBlockOffset = reader.BaseStream.Position;

					reader.BaseStream.Seek(stringBlockOffset, SeekOrigin.Begin);
					Array.Resize<byte>(ref buffer, (int)(reader.BaseStream.Length - stringBlockOffset));
					reader.Read(buffer, 0, buffer.Length);
					string stringBlock = Encoding.ASCII.GetString(buffer);

					reader.BaseStream.Seek(sectionBlockOffset, SeekOrigin.Begin);
					while (reader.BaseStream.Position < stringBlockOffset) sections.Add(new Section(reader, stringBlock));
				}
				else // Text Ini
				{
					stream.Seek(0, SeekOrigin.Begin);
					StreamReader reader = new StreamReader(stream);

					int currentSection = -1;
					int currentLine = 0;
					while (!reader.EndOfStream)
					{
						string line = reader.ReadLine().Trim();

						if (string.IsNullOrWhiteSpace(line) 
							|| line.StartsWith(";", StringComparison.OrdinalIgnoreCase)
							|| line.StartsWith("@", StringComparison.OrdinalIgnoreCase)) 
							continue;

						if (line.StartsWith("[", StringComparison.OrdinalIgnoreCase))
						{
							if (!line.Contains("]")) throw new FileContentException(path, IniFileType, "Invalid section header: " + line);

							string name = line.Substring(1);
							currentSection++;
							sections.Add(new Section(name.Remove(name.IndexOf(']')).Trim()));

							continue;
						}
						else
						{
							if (currentSection < -1) throw new FileContentException(path, IniFileType, "Entry before first section: " + line);
							if (line.Contains(";")) line = line.Remove(line.IndexOf(";", StringComparison.OrdinalIgnoreCase)).TrimEnd();
							if (!char.IsLetterOrDigit (line [0]) && line [0] != '_') {
								FLLog.Warning ("Ini", "Invalid line in file: " + path + " at line " + currentLine + '"' + line + '"');
							}
							else if (line.Contains("="))
							{
								string[] parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
								if (parts.Length == 2) {
									string val = parts [1].TrimStart ();
									string[] valParts;
									if (val.Contains (","))
										valParts = val.Split (new char[] { ',' });
									else
										valParts = new string[] { val };

									List<IValue> values = new List<IValue> ();
									foreach (string part in valParts) {
										string s = part.Trim ();
										bool tempBool;
										int tempInt;
										float tempFloat;

										if (bool.TryParse (s, out tempBool))
											values.Add (new BooleanValue (tempBool));
										else if (int.TryParse (s, out tempInt))
											values.Add (new Int32Value (tempInt));
										else if (float.TryParse (s, out tempFloat))
											values.Add (new SingleValue (tempFloat));
										else
											values.Add (new StringValue (s));
									}

									sections [currentSection].Add (new Entry (parts [0].TrimEnd (), values));
								} else if (parts.Length == 3 && allowmaps) {
									string k = parts [1].Trim ();
									string v = parts [2].Trim ();

								} else if (parts.Length == 1) {
									sections [currentSection].Add (new Entry (parts [0].Trim (), new List<IValue> ()));
								}
								else throw new FileContentException(path, IniFileType, "Invalid entry line: " + line);
							}
							else sections[currentSection].Add(new Entry(line, new List<IValue>(0)));
						}
						currentLine++;
					}
				}
			}

			return sections;
		}

		/*public Section this[int index]
        {
            get { return sections[index]; }
        }

        public Section this[string name]
        {
            get
            {
                IEnumerable<Section> candidates = from Section s in sections where s.Name.Equals(name, StringComparison.OrdinalIgnoreCase) select s;
                int count = candidates.Count<Section>();
                if (count == 0) return null;
                else return candidates.First<Section>();
            }
        }

        public void Add(Section item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(Section item)
        {
            return sections.Contains(item);
        }

        public void CopyTo(Section[] array, int arrayIndex)
        {
            sections.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return sections.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(Section item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<Section> GetEnumerator()
        {
            return sections.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return sections.GetEnumerator();
        }

        public override string ToString()
        {
            string result = string.Empty;
            foreach (Section s in sections) result += s + "\r\n";
            return result;
        }*/
	}
}