// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LibreLancer.Data.Ini;

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
            {
                return false;
            }
        }
        return true;
    }

    private static void ParseKeyValue(Section section, string key, ReadOnlySpan<char> value, int line, bool preparse, bool allowmaps, IniStringPool stringPool)
    {
        Span<Range> parts = stackalloc Range[256];
        Entry entry;
        var kvIdx = value.IndexOf('=');
        if (allowmaps && kvIdx >= 0)
        {
            entry = new Entry(section, key, 1) { Line = line };
            value.Split(parts, '=', StringSplitOptions.TrimEntries);
            var (v1Start, v1Length) = parts[0].GetOffsetAndLength(value.Length);
            var (v2Start, v2Length) = parts[1].GetOffsetAndLength(value.Length);
            entry.Add(new StringKeyValue(
                stringPool.FromSpan(value.Slice(v1Start, v1Length)),
                stringPool.FromSpan(value.Slice(v2Start, v2Length))) { Entry = entry });
        }
        else
        {
            int valueCount = value.Split(parts, ',', StringSplitOptions.TrimEntries);
            entry = new Entry(section, key, valueCount) { Line = line };
            for(int i = 0; i < valueCount; i++)
            {
                // valueCount > 1 = we can have some empty values
                // otherwise we want to skip if the entire value section is empty
                if ((valueCount > 1 && parts[i].End.Value >= parts[i].Start.Value) ||
                    (parts[i].End.Value > parts[i].Start.Value))
                {
                    var (vStart, vLength) = parts[i].GetOffsetAndLength(value.Length);
                    var part = value.Slice(vStart, vLength);
                    if (preparse && part.Length > 0 && (part[0] == '-' || part[0] >= '0' && part[0] <= '9'))
                    {
                        bool isLong = long.TryParse(part, out long tempLong);
                        if (isLong && tempLong >= int.MinValue && tempLong <= int.MaxValue)
                        {
                            entry.Add(new Int32Value((int)tempLong) { Entry = entry, Line = line });
                        }
                        else if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float tempFloat))
                        {
                            entry.Add(new SingleValue(tempFloat, isLong ? tempLong : null) { Entry = entry, Line = line });
                        }
                        else
                        {
                            entry.Add(new LancerStringValue(stringPool.FromSpan(part)) { Entry = entry, Line = line });
                        }
                    }
                    else if (preparse && bool.TryParse(part, out bool tempBool))
                    {
                        entry.Add(new BooleanValue(tempBool) { Entry = entry, Line = line });
                    }
                    else
                    {
                        entry.Add(new LancerStringValue(stringPool.FromSpan(part)) { Entry = entry, Line = line });
                    }
                }
            }
        }
        section.Add(entry);
    }

    public IEnumerable<Section> ParseIniFile(string? path,
        Stream stream,
        bool preparse = true,
        bool allowmaps = false,
        IniStringPool? stringPool = null)
    {
        // Ensure that we parse in code page windows-1252 if there is no BOM.
        var reader = new StreamReader(stream,
            encoding: defaultEncoding,
            detectEncodingFromByteOrderMarks: true);

        Section? currentSection = null;
        int currentLine = 0;
        bool inSection = false;
        stringPool ??= new IniStringPool();

        while (!reader.EndOfStream)
        {
            currentLine++;
            var line = reader.ReadLine().AsSpan().Trim(spacesAndTabs);

            // Quickly discard lines that we know contain nothing useful
            if (line.IsWhiteSpace() || line[0] == ';' || line[0] == '@')
            {
                continue;
            }

            if (line[0] == '[')
            {
                var toReturn = currentSection;
                inSection = true;

                var sectionIdx = line.IndexOf(']') - 1;
                if (sectionIdx < 0)
                {
                    sectionIdx = line.Length - 1;
                }

                currentSection = new Section(stringPool.FromSpan(line.Slice(1, sectionIdx).TrimEnd(spacesAndTabs)))
                    { File = path, Line = currentLine };
                if (toReturn != null)
                {
                    yield return toReturn;
                }

                continue;
            }

            if (!inSection)
            {
                continue;
            }

            int commentIndex = line.IndexOf(';');
            if (commentIndex >= 0)
            {
                line = line.Slice(0, commentIndex);
            }

            if (line.IsWhiteSpace())
            {
                continue;
            }

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex >= 0)
            {
                var key = line.Slice(0, equalsIndex).Trim(spacesAndTabs);
                var value = line.Slice(equalsIndex + 1).Trim();
                ParseKeyValue(currentSection!, stringPool.FromSpan(key), value, currentLine, preparse, allowmaps, stringPool);
            }
            else
            {
                var key = stringPool.FromSpan(line.Trim(spacesAndTabs));
                currentSection!.Add(new Entry(currentSection, key));
            }
        }

        if (currentSection != null)
        {
            yield return currentSection;
        }
    }
}
