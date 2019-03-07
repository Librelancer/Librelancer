// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer.Data;

namespace LibreLancer.Ini
{
	public abstract partial class IniFile
	{
		public const string FileType = "BINI", IniFileType = "INI";
		public const int FileVersion = 1;
        static int ParseEquals(string line, string[] part, bool allowmaps)
        {
            var idx0 = line.IndexOf('=');
            if (idx0 == -1)
            {
                part[0] = line;
                return 1;
            }

            part[0] = line.Substring(0, idx0);
            if ((idx0 + 1) >= line.Length) return 1;
            var idx1 = line.IndexOf('=', idx0 + 1);
            if (idx1 != -1 && !allowmaps)
            {
                //Skip duplicate equals
                for (int i = idx0; i < line.Length; i++)
                {
                    if (line[i] != '=' && line[i] != ' ' && line[i] != '\t')
                    {
                        idx0 = i - 1;
                        break;
                    }
                }
            }
            else if (idx1 != -1)
            {
                part[1] = line.Substring(idx0 + 1, idx1 - (idx0 + 1));
                part[2] = line.Substring(idx1 + 1);
                return 3;
            }
            part[1] = line.Substring(idx0 + 1);
            return 2;
        }

        protected IEnumerable<Section> ParseFile(string path, MemoryStream stream, bool allowmaps = false)
        {
            if (string.IsNullOrEmpty(path)) path = "[Memory]";
            stream.Position = 0;
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
                string[] parts = new string[3];
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
                        currentSection = new Section(name) { File = path, Line = currentLine };

                        inSection = true;
                        continue;
                    }
                    else
                    {
                        if (!inSection) continue;
                        int indexComment = line.IndexOf(';');
                        if (indexComment != -1) line = line.Remove(indexComment);
                        int partCount;
                        if (!char.IsLetterOrDigit(line[0]) && line[0] != '_')
                        {
                            FLLog.Warning("Ini", "Invalid line in file: " + path + " at line " + currentLine + '"' + line + '"');
                        }
                        else
                        {
                            partCount = ParseEquals(line, parts, allowmaps);
                            if (partCount == 2)
                            {
                                string val = parts[1];
                                string[] valParts = val.Split(',');

                                List<IValue> values = new List<IValue>(valParts.Length);
                                foreach (string part in valParts)
                                {
                                    string s = part.Trim();
                                    bool tempBool;
                                    float tempFloat;
                                    long tempLong;
                                    if (part.Length == 0)
                                    {
                                        values.Add(new StringValue(""));
                                        continue;
                                    }
                                    if (part[0] == '-' || (part[0] >= '0' && part[0] <= '9'))
                                    {
                                        if (long.TryParse(s, out tempLong))
                                        {
                                            if (tempLong >= int.MinValue && tempLong <= int.MaxValue)
                                                values.Add(new Int32Value((int)tempLong));
                                            else
                                                values.Add(new SingleValue(tempLong, tempLong));
                                        }
                                        else if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out tempFloat))
                                        {
                                            values.Add(new SingleValue(tempFloat, null));
                                        }
                                        else
                                            values.Add(new StringValue(s));
                                    }
                                    else if (bool.TryParse(s, out tempBool))
                                        values.Add(new BooleanValue(tempBool));
                                    else
                                        values.Add(new StringValue(s));
                                }

                                currentSection.Add(new Entry(parts[0].Trim(), values) { File = path, Line = currentLine });
                            }
                            else if (partCount == 3 && allowmaps)
                            {
                                string k = parts[1].Trim();
                                string v = parts[2].Trim();
                                currentSection.Add(new Entry(parts[0].Trim(), new IValue[] { new StringKeyValue(k, v) }) { File = path, Line = currentLine });
                            }
                            else if (partCount == 1)
                            {
                                currentSection.Add(new Entry(parts[0].Trim(), new List<IValue>()) { File = path, Line = currentLine });
                            }
                        }
                    

                    }
                    currentLine++;
                }
            }
            if (currentSection != null) yield return currentSection;
        }
		protected IEnumerable<Section> ParseFile(string path, bool allowmaps = false)
		{
			if (path == null) throw new ArgumentNullException("path");
			if (!path.ToLowerInvariant().EndsWith(".ini"))
				path = path + ".ini";
            using (var stream = new MemoryStream())
            {
                //Don't wait on I/O for yield return
                using (Stream file = VFS.Open(path)) {
                    file.CopyTo(stream);
                }
                foreach (var s in ParseFile(path, stream, allowmaps)) yield return s;
            }
		}
	}
}