// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace LibreLancer.Ini
{
    public class TextIniParser : IIniParser
    {
        private const string INI = "INI";

        private static int ParseEquals(string line, string[] part, bool allowmaps)
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

        public bool CanParse(Stream stream)
        {
            var buffer = new byte[16];
            var length = stream.Read(buffer, 0, buffer.Length);
            stream.Seek(0, SeekOrigin.Begin);
            for (var i = 0; i < length; i++)
            {
                var c = buffer[i];
                if (!(c < 0x80 && (c >= 0x32 || c == 0xd || c == 0xa)))
                    return false;
            }
            return true;
        }

        public IEnumerable<Section> ParseIniFile(string path,
            Stream stream,
            bool preparse = true,
            bool allowmaps = false)
        {
            Section currentSection = null;
            var reader = new StreamReader(stream);

            int currentLine = 0;
            bool inSection = false;
            string[] parts = new string[3];
            while (!reader.EndOfStream)
            {
                currentLine++;
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
                    if (indexClose == -1)
                        throw new FileContentException(path, INI, "Invalid section header: " + line);
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
                        var entry = new Entry(currentSection, parts[0].Trim()) { Line = currentLine,  };
                        if (partCount == 2)
                        {
                            string val = parts[1];
                            string[] valParts = val.Split(',');

                            foreach (string part in valParts)
                            {
                                string s = part.Trim();
                                if (s.Length == 0)
                                {
                                    entry.Add(new StringValue("") { Entry = entry, Line = currentLine});
                                    continue;
                                }
                                if (preparse && (s[0] == '-' || s[0] >= '0' && s[0] <= '9'))
                                {
                                    if (long.TryParse(s, out long tempLong))
                                    {
                                        if (tempLong >= int.MinValue && tempLong <= int.MaxValue)
                                            entry.Add(new Int32Value((int)tempLong) { Entry = entry, Line = currentLine });
                                        else
                                            entry.Add(new SingleValue(tempLong, tempLong) { Entry = entry, Line = currentLine });
                                    }
                                    else if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float tempFloat))
                                    {
                                        entry.Add(new SingleValue(tempFloat, null) { Entry = entry , Line = currentLine });
                                    }
                                    else
                                        entry.Add(new StringValue(s) { Entry = entry, Line = currentLine });
                                }
                                else if (preparse && bool.TryParse(s, out bool tempBool))
                                    entry.Add(new BooleanValue(tempBool) { Entry = entry, Line = currentLine });
                                else
                                    entry.Add(new StringValue(s) { Entry = entry, Line = currentLine });
                            }
                            currentSection.Add(entry);
                        }
                        else if (partCount == 3 && allowmaps)
                        {
                            string k = parts[1].Trim();
                            string v = parts[2].Trim();
                            entry.Add(new StringKeyValue(k, v) { Entry = entry, Line = currentLine });
                            currentSection.Add(entry);
                        }
                        else if (partCount == 1)
                        {
                            currentSection.Add(entry);
                        }
                    }
                }
            }
            if (currentSection != null) yield return currentSection;
        }
    }
}
