// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibreLancer.Ini
{
    /// <summary>
    /// Implementation of a text ini parser which comes as close to the original as possible.
    /// </summary>
    /// <remarks>
    /// Freelancer data contains a mixture of binary ini and text ini files. The text ini files
    /// seem to contain nbsp (0xa0) characters from the windows-1252 code page which can be seen in
    /// DATA/initialworld.ini. The means that unless the ini file has BOM we should interpret the
    /// file data as from this code page.
    /// </remarks>
    public class LancerTextIniParser : IIniParser
    {
        private const int defaultCodePage = 1252;
        private static readonly Encoding defaultEncoding;
        private static readonly char[] spacesAndTabs = [' ', '\t'];

        static LancerTextIniParser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            defaultEncoding = Encoding.GetEncoding(defaultCodePage);
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

        private static void ParseKeyValue(Section section, string key, string value, int line, bool preparse, bool allowmaps)
        {
            var entry = new Entry(section, key) { Line = line };

            var values = new List<IValue>();
            var kvIdx = value.IndexOf('=');
            if (allowmaps && kvIdx >= 0)
            {
                var parts = value.Split('=');
                entry.Add(new StringKeyValue(parts[0].Trim(), parts[1].Trim()) { Entry = entry });
            }
            else
            {
                foreach (string part in value.Split(",").Select(p => p.Trim()))
                {
                    if (!string.IsNullOrEmpty(part))
                    {
                        if (preparse && (part[0] == '-' || part[0] >= '0' && part[0] <= '9'))
                        {
                            if (long.TryParse(part, out long tempLong)
                                && tempLong >= int.MinValue && tempLong <= int.MaxValue)
                            {
                                entry.Add(new Int32Value((int)tempLong) { Entry = entry, Line = line });
                            }
                            else if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float tempFloat))
                            {
                                entry.Add(new SingleValue(tempFloat, null) { Entry = entry, Line = line });
                            }
                            else
                                entry.Add(new LancerStringValue(part) { Entry = entry, Line = line });
                        }
                        else if (preparse && bool.TryParse(part, out bool tempBool))
                            entry.Add(new BooleanValue(tempBool) { Entry = entry, Line = line });
                        else
                            entry.Add(new LancerStringValue(part) { Entry = entry, Line = line });
                    }
                }
            }
            section.Add(entry);
        }

        public IEnumerable<Section> ParseIniFile(string path,
            Stream stream,
            bool preparse = true,
            bool allowmaps = false)
        {
            // Ensure that we parse in code page windows-1252 if there is no BOM.
            var reader = new StreamReader(stream,
                encoding: defaultEncoding,
                detectEncodingFromByteOrderMarks: true);

            Section currentSection = null;
            int currentLine = 0;
            bool inSection = false;

            while (!reader.EndOfStream)
            {
                currentLine++;
                string line = reader.ReadLine().Trim(spacesAndTabs);

                // Quickly discard lines that we know contain nothing useful
                if (string.IsNullOrWhiteSpace(line)
                    || line[0] == ';'
                    || line[0] == '@')
                    continue;

                for (var i = 0; i < line.Length; i++)
                {
                    if (line[i] == '[')
                    {
                        if (currentSection != null) yield return currentSection;
                        inSection = true;

                        var sectionIdx = line.IndexOf(']', i) - 1;
                        if (sectionIdx < 0) sectionIdx = line.Length - 1;
                        currentSection = new Section(line.Substring(i + 1, sectionIdx - i).TrimEnd(spacesAndTabs))
                        { File = path, Line = currentLine };
                        break;
                    }
                    if (inSection && line[i] == '=')
                    {
                        var key = line.Substring(0, i).Trim(spacesAndTabs);
                        var idx = line.IndexOf(';', i);
                        if (idx != -1)
                            ParseKeyValue(currentSection,
                                key,
                                line.Substring(i + 1, idx - i - 1).Trim(),
                                currentLine, preparse, allowmaps);
                        else
                            ParseKeyValue(currentSection,
                                key,
                                line.Substring(i + 1).Trim(),
                                currentLine, preparse, allowmaps);
                        break;
                    }
                    else if (inSection && i == line.Length - 1)
                    {
                        var key = line;
                        if (!string.IsNullOrEmpty(key))
                        {
                            ParseKeyValue(currentSection, key, "", currentLine, preparse, allowmaps);
                        }
                        break;
                    }
                    else if (inSection && line[i] == ';')
                    {
                        var key = line.Substring(0, i).Trim();
                        if (!string.IsNullOrEmpty(key))
                        {
                            ParseKeyValue(currentSection, key, "", currentLine, preparse, allowmaps);
                        }
                        break;
                    }
                    if (line[i] == ';') break;
                }
            }
            if (currentSection != null) yield return currentSection;
        }
    }
}
