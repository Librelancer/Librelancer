// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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

		protected IEnumerable<Section> ParseFile(string path, bool allowmaps = false)
		{
			if (path == null) throw new ArgumentNullException("path");

			//List<Section> sections = new List<Section>();
			if (!path.ToLowerInvariant().EndsWith(".ini"))
				path = path + ".ini";
            using (Stream stream = VFS.Open(path))
			{
				byte[] buffer = new byte[4];
				stream.Read(buffer, 0, 4);
				string fileType = Encoding.ASCII.GetString(buffer);
                Section currentSection = null;
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
                    while (reader.BaseStream.Position < stringBlockOffset) yield return new Section(reader, stringBlock);
				}
				else // Text Ini
				{
					stream.Seek(0, SeekOrigin.Begin);
					StreamReader reader = new StreamReader(stream);

					int currentLine = 0;
					bool inSection = false;
					while (!reader.EndOfStream)
					{
						string line = reader.ReadLine().Trim();

						if (string.IsNullOrWhiteSpace(line) 
                            || line[0] == ';'
                            || line[0] == '@') 
							continue;

						if (line[0] == '[')
						{
                            if (currentSection != null) yield return currentSection;
                            int indexComment = line.IndexOf(';');
                            int indexClose = line.IndexOf(']');
                            if (indexComment != -1 && indexComment < indexClose)
							{
								inSection = false;
                                currentSection = null;
								continue;
							}
							if (indexClose == -1) throw new FileContentException(path, IniFileType, "Invalid section header: " + line);
                            string name = line.Substring(1, indexClose - 1).Trim();
                            currentSection = new Section(name);
							inSection = true;
							continue;
						}
						else
						{
							if (!inSection) continue;
                            int indexComment = line.IndexOf(';');
                            if (indexComment != -1) line = line.Remove(indexComment);
							if (!char.IsLetterOrDigit (line [0]) && line [0] != '_') {
								FLLog.Warning ("Ini", "Invalid line in file: " + path + " at line " + currentLine + '"' + line + '"');
							}
							else if (line.Contains("="))
							{
								string[] parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
								if (parts.Length == 2) {
									string val = parts [1].TrimStart ();
                                    string[] valParts = val.Split(',');

                                    List<IValue> values = new List<IValue>(valParts.Length);
									foreach (string part in valParts) {
										string s = part.Trim ();
										bool tempBool;
										float tempFloat;
                                        long tempLong;
                                        if (bool.TryParse(s, out tempBool))
                                            values.Add(new BooleanValue(tempBool));
                                        else if (long.TryParse(s, out tempLong))
                                        {
                                            if (tempLong >= int.MinValue && tempLong <= int.MaxValue)
                                                values.Add(new Int32Value((int)tempLong));
                                            else
                                                values.Add(new SingleValue(tempLong, tempLong));
                                        }
										else if (float.TryParse(s, out tempFloat))
										{
                                            values.Add(new SingleValue(tempFloat, null));
										}
										else
											values.Add (new StringValue (s));
									}

									currentSection.Add (new Entry (parts [0].TrimEnd (), values));
								} else if (parts.Length == 3 && allowmaps) {
									string k = parts [1].Trim ();
									string v = parts [2].Trim ();
									currentSection.Add(new Entry(parts[0].Trim(), new IValue[] { new StringKeyValue(k,v) }));
								} else if (parts.Length == 1) {
									currentSection.Add (new Entry (parts [0].Trim (), new List<IValue> ()));
								}
								else FLLog.Error("INI", "Invalid entry line: " + line + " in " + path);
							}
							else currentSection.Add(new Entry(line, new List<IValue>(0)));
						}
						currentLine++;
					}
				}
                if (currentSection != null) yield return currentSection;
			}
		}
	}
}