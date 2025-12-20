using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LibreLancer.ContentEdit.RandomMissions;
using LibreLancer.Data.Dll;

namespace LibreLancer.ContentEdit.Frc;

public static class FrcCompiler
{
    static FrcCompiler()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static bool IsSpace(char c) => c is ' ' or '\t';

    public const string InfocardStart = "<?xml version=\"1.0\" encoding=\"UTF-16\"?><RDL><PUSH/>";
    public const string InfocardEnd = "<POP/></RDL>";

    // Color Tables
    // NOTE: These are stored for writing with ToString().
    // NOT in render component order (which ends up being 0xAABBGGRR)
    private static Dictionary<char, uint> singleCharColors = new()
    {
        { 'z', 0 },
        { 'r', 0xFF0000 },
        { 'g', 0x00FF00 },
        { 'b', 0x0000FF },
        { 'c', 0x00FFFF },
        { 'm', 0xFF00FF },
        { 'y', 0xFFFF00 },
        { 'w', 0xFFFFFF }
    };

    private static Dictionary<uint, string> colorNames = new()
    {
        { 0x808080, "Gray" },
        { 0x4848E0, "Blue" },
        { 0x3BBF1D, "Green" },
        { 0x87C3E0, "Aqua" },
        { 0xBF1D1D, "Red" },
        { 0x8800C2, "Fuchsia" },
        { 0xF5EA52, "Yellow" },
        { 0xFFFFFF, "White" }
    };

    /// <summary>
    /// Reads in all text of a file, with support for win-1252 encoded files as well as Unicode.
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>The decoded text</returns>
    public static string ReadAllText(string path)
    {
        var x = File.ReadAllText(path).ReplaceLineEndings("\n");
        if (x.Contains('\uFFFD')) // utf decode failed
        {
            var win1252 = Encoding.GetEncoding(1252);
            return File.ReadAllText(path, win1252).ReplaceLineEndings("\n");
        }
        return x;
    }

    public static ResourceDll Compile(string text, string source, int resourceIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentNullException(nameof(text));
        }

        ResourceDll res = new();

        var reader = new FrcReader(text, source);

        while (reader.Current() != -1)
        {
            if (!SkipWhitespace(reader))
                break;
            if (reader.Current() == ';')
            {
                SkipComment(reader);
                continue;
            }
            var defCol = reader.Column;
            var defLine = reader.Line;
            switch (reader.Current())
            {
                case 'S':
                {
                    var (ids, str) = ReadElement(reader, source, false, resourceIndex);
                    if (!res.Strings.TryAdd((ids & 0xFFFF), str) || res.Infocards.ContainsKey(ids & 0xFFFF))
                    {
                        throw new CompileErrorException(source, defCol, defLine,
                            $"Duplicate definition of {ids} ({ids & 0xFFFF})");
                    }
                    break;
                }
                case 'H':
                case 'I':
                {
                    var (ids, str) = ReadElement(reader, source, true, resourceIndex);
                    if (!res.Infocards.TryAdd((ids & 0xFFFF), str) || res.Strings.ContainsKey(ids & 0xFFFF))
                    {
                        throw new CompileErrorException(source, defCol, defLine,
                            $"Duplicate definition of {ids} ({ids & 0xFFFF})");
                    }
                    break;
                }
                case 'L':
                {
                    // Ignore language code (we always output 1033).
                    SkipLine(reader);
                    break;
                }
                default:
                    throw new CompileErrorException(source, reader.Column, reader.Line,
                        $"Unknown directive '{(char)reader.Current()}', expected S, L, H, or I.");
            }
        }

        return res;
    }

    static (int IDS, string String) ReadElement(FrcReader reader, string source, bool rdl, int resourceIndex)
    {
        // S/H/I
        reader.Advance();
        // Move to ID
        if (!IsSpace((char)reader.Current()))
            UnexpectedError(reader, source, "space");
        while (reader.Current() != -1 && IsSpace((char)reader.Current()))
            reader.Advance();
        if (reader.Current() == -1 || !char.IsAsciiDigit((char)reader.Current()))
            UnexpectedError(reader, source, "ID");
        // Read ID
        var ids = ReadIDS(reader, source, resourceIndex);
        // String contents can be N spaces after ids, or line after ids (with N spaces)
        // Skip whitespace #1
        while (reader.Current() != -1 && IsSpace((char)reader.Current()))
            reader.Advance();
        // Skip starting '\n'
        if (reader.Current() == '\n')
        {
            reader.Advance();
        }

        // Skip whitespace #2
        while (reader.Current() != -1 && IsSpace((char)reader.Current()))
            reader.Advance();
        if (reader.Current() == '~')
        {
            //~ == invert
            reader.Advance();
            return (ids, ReadContents(reader, source, !rdl));
        }

        if (reader.Current() == -1 || IsDirective(reader))
        {
            // Element has no contents
            return rdl
                ? (ids, $"{InfocardStart}{InfocardEnd}")
                : (ids, "");
        }

        return (ids, ReadContents(reader, source, rdl));
    }

    static int ReadIDS(FrcReader reader, string source, int resourceIndex)
    {
        var sb = new StringBuilder();
        while (reader.Current() != -1 &&
               char.IsAsciiDigit((char)reader.Current()))
        {
            sb.Append((char)reader.Current());
            reader.Advance();
        }

        var ids = int.Parse(sb.ToString());
        // Range check
        if (resourceIndex != -1)
        {
            var min = resourceIndex * 65536;
            var max = min + 65535;
            if (ids < min || ids > max)
            {
                throw new CompileErrorException(source, reader.Column, reader.Line,
                    $"{ids} is out of range ({min}, {max})");
            }
        }
        return ids;
    }

    static bool IsDirective(FrcReader reader)
    {
        var c = reader.Current();
        return reader.Column == 1 &&
               (c == 'S' || c == 'H' || c == 'I');
    }

    static char Consume(FrcReader reader, string source, string expected, Func<char, bool> predicate)
    {
        if (reader.Current() == -1)
            UnexpectedError(reader, source, expected);
        var ch = (char)reader.Current();
        if (!predicate(ch))
            UnexpectedError(reader, source, expected);
        reader.Advance();
        return ch;
    }

    struct TRAState
    {
        public bool? Bold;
        public bool? Italic;
        public bool? Underline;
        public uint? Color;
        public uint? FontNumber;
    }

    static void WriteFormattingTags(StringBuilder final, TRAState prev, TRAState cur,
        char lastJust, char curJust, ref bool inText)
    {
        string TristateBool(bool? b) =>
            b == null ? "\"default\"" : b.Value ? "\"true\"" : "\"false\"";

        StringBuilder traTag = new();
        if (prev.Bold != cur.Bold)
        {
            traTag.Append($" bold={TristateBool(cur.Bold)}");
        }

        if (prev.Italic != cur.Italic)
        {
            traTag.Append($" italic={TristateBool(cur.Italic)}");
        }

        if (prev.Underline != cur.Underline)
        {
            traTag.Append($" underline={TristateBool(cur.Underline)}");
        }

        if (prev.Color != cur.Color)
        {
            if (cur.Color.HasValue)
            {
                if (colorNames.TryGetValue(cur.Color.Value, out var c))
                {
                    traTag.Append($" color=\"{c.ToLowerInvariant()}\"");
                }
                else
                {
                    traTag.Append($" color=\"#{cur.Color.Value:X6}\"");
                }
            }
            else
            {
                traTag.Append(" color=\"default\"");
            }
        }

        if (prev.FontNumber != cur.FontNumber)
        {
            if (cur.FontNumber.HasValue)
            {
                traTag.Append($" font=\"{cur.FontNumber.Value}\"");
            }
            else
            {
                traTag.Append(" font=\"default\"");
            }
        }

        if (lastJust != curJust)
        {
            if (inText)
            {
                final.Append("</TEXT>");
                inText = false;
            }

            final.Append($"<JUST loc=\"{(curJust == 'm' ? 'c' : curJust)}\"/>");
        }

        if (traTag.Length > 0)
        {
            if (inText)
            {
                final.Append("</TEXT>");
                inText = false;
            }

            final.Append("<TRA");
            final.Append(traTag);
            final.Append("/>");
        }
    }

    static uint ReadColor(FrcReader reader, string source)
    {
        // Named Colors
        foreach (var kv in colorNames)
        {
            if (reader.Match(kv.Value))
            {
                reader.Advance(kv.Value.Length);
                return kv.Key;
            }
        }
        // Hex colors
        var col0 = (char)reader.Current();
        if (char.IsAsciiHexDigitUpper(col0))
        {
            Span<char> colorDigits = stackalloc char[6];
            colorDigits[0] = col0;
            reader.Advance();
            colorDigits[1] = Consume(reader, source, "hex digit (upper case)",
                char.IsAsciiHexDigitUpper);
            colorDigits[2] = Consume(reader, source, "hex digit (upper case)",
                char.IsAsciiHexDigitUpper);
            colorDigits[3] = Consume(reader, source, "hex color (upper case)",
                char.IsAsciiHexDigitUpper);
            colorDigits[4] = Consume(reader, source, "hex color (upper case)",
                char.IsAsciiHexDigitUpper);
            colorDigits[5] = Consume(reader, source, "hex color (upper case)",
                char.IsAsciiHexDigitUpper);
            return uint.Parse(colorDigits, NumberStyles.HexNumber);
        }
        // Single char colors (optional modifiers)
        uint mask = 0xFFFFFF;
        if (col0 == 'd')
        {
            mask = 0x404040;
            reader.Advance();
        }
        else if (col0 == 'h')
        {
            mask = 0x808080;
            reader.Advance();
        }
        else if (col0 == 'l')
        {
            mask = 0xC0C0C0;
            reader.Advance();
        }

        if (!singleCharColors.TryGetValue((char)reader.Current(), out var colorValue))
            UnexpectedError(reader, source, "color");
        reader.Advance(); //Single char color
        return mask & colorValue;
    }

    static string ReadContents(FrcReader reader, string source, bool rdl)
    {
        var final = new StringBuilder();
        if (rdl)
        {
            final.Append(InfocardStart);
        }

        int prev = -1;
        //stackalloc must be outside of loop
        Span<char> unicodeDigits = stackalloc char[4];

        // Defer adding whitespace to the main StringBuilder
        // so that we can trim trailing whitespace (unless requested by the file).

        bool isLineStart = true; // Trim whitespace at start of each line
        int deferredNewLine = 0; // \n characters to insert
        var whitespaceBuffer = new StringBuilder();
        bool rdlInText = false;

        void NewLine()
        {
            if (rdl && rdlInText)
            {
                final.Append("</TEXT>");
                rdlInText = false;
            }

            final.Append(rdl ? "<PARA/>" : "\n");
        }

        void AppendChar(char c) => AppendText(c.ToString());

        void AppendText(string c)
        {
            if (rdl && !rdlInText)
            {
                final.Append("<TEXT>");
                rdlInText = true;
            }

            final.Append(c);
        }

        void FlushWhitespace()
        {
            if (final.Length > 0) // Always trim whitespace from start of string
            {
                if (whitespaceBuffer.Length > 0)
                    AppendText(whitespaceBuffer.ToString());
                for (int i = 0; i < deferredNewLine; i++)
                    NewLine();
                deferredNewLine = 0;
            }

            isLineStart = false;
            deferredNewLine = 0;
            whitespaceBuffer.Clear();
        }

        void Finalize(bool terminated = false)
        {
            if (rdl)
            {
                if (rdlInText)
                {
                    final.Append("</TEXT>");
                }

                if (!terminated)
                {
                    final.Append("<PARA/>");
                }

                final.Append(InfocardEnd);
            }
        }

        TRAState applied = new();
        RefList<TRAState> traStk = new();
        traStk.Add(applied);
        char lastJust = '\0';
        char curJust = '\0';

        ref TRAState RDLBlock(FrcReader reader, bool advance = true)
        {
            if (advance)
                reader.Advance();
            if (reader.Current() == '{')
            {
                reader.Advance();
                traStk.Add(traStk[^1]);
            }

            return ref traStk[^1];
        }

        void ProcessRDL()
        {
            if (!rdl)
                return;
            WriteFormattingTags(final, applied, traStk[^1], lastJust, curJust, ref rdlInText);
            applied = traStk[^1];
            curJust = lastJust;
        }

        while (reader.Current() != -1 &&
               !IsDirective(reader))
        {
            var cur = (char)reader.Current();

            //Finish if we encounter a comment
            if (cur == ';' && (IsSpace((char)prev) || reader.Column == 1))
            {
                SkipComment(reader);
                Finalize();
                return final.ToString();
            }

            if (cur == '\\')
            {
                FlushWhitespace();
                reader.Advance();
                if (reader.Current() == -1)
                {
                    break; // Just for adding whitespace
                }

                switch ((char)reader.Current())
                {
                    case '\\':
                        ProcessRDL();
                        final.Append('\\');
                        break;
                    case 'n':
                        NewLine();
                        break;
                    case '\n':
                    {
                        // Start new line without inserting \n
                        isLineStart = true;
                        deferredNewLine = 0;
                        break;
                    }
                    case 'x':
                    {
                        ProcessRDL();
                        reader.Advance();
                        unicodeDigits[0] = Consume(reader, source, "hex digit", char.IsAsciiHexDigit);
                        unicodeDigits[1] = Consume(reader, source, "hex digit", char.IsAsciiHexDigit);
                        unicodeDigits[2] = Consume(reader, source, "hex digit", char.IsAsciiHexDigit);
                        unicodeDigits[3] = Consume(reader, source, "hex digit", char.IsAsciiHexDigit);
                        prev = unicodeDigits[3];
                        AppendText(char.ConvertFromUtf32(int.Parse(unicodeDigits, NumberStyles.HexNumber)));
                        continue; //Skip advance
                    }
                    case '.':
                        reader.Advance();
                        Finalize(true); //terminate without final <PARA/>
                        return final.ToString();
                    case '1':
                        ProcessRDL();
                        AppendChar('\u2081');
                        break;
                    case '2':
                        ProcessRDL();
                        AppendChar('\u2082');
                        break;
                    case '3':
                        ProcessRDL();
                        AppendChar('\u2083');
                        break;
                    case '0':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        ProcessRDL();
                        AppendChar((char)(reader.Current() - '0' + '\u2070'));
                        break;
                    //Raw RDL escape
                    case '<' when rdl:
                        ProcessRDL(); //Clear up to this point
                        do
                        {
                            final.Append((char)reader.Current());
                            reader.Advance();
                        } while (reader.Current() != -1 && reader.Current() != '>');
                        if (reader.Current() != -1)
                        {
                            final.Append('>');
                            reader.Advance();
                        }
                        continue;
                    //RDL Formatting.
                    case 'b' when rdl:
                        RDLBlock(reader).Bold = true;
                        continue;
                    case 'B' when rdl:
                        RDLBlock(reader).Bold = false;
                        continue;
                    case 'i' when rdl:
                        RDLBlock(reader).Italic = true;
                        continue;
                    case 'I' when rdl:
                        RDLBlock(reader).Italic = false;
                        continue;
                    case 'u' when rdl:
                        RDLBlock(reader).Underline = true;
                        continue;
                    case 'U' when rdl:
                        RDLBlock(reader).Underline = false;
                        continue;
                    case 'F' when rdl:
                        RDLBlock(reader).FontNumber = null;
                        continue;
                    case 'C' when rdl:
                        RDLBlock(reader).Color = null;
                        continue;
                    case 'c' when rdl:
                    {
                        reader.Advance();
                        var col = ReadColor(reader, source);
                        RDLBlock(reader, false).Color = col;
                        continue;
                    }
                    case 'f' when rdl:
                    {
                        reader.Advance();
                        char d0 = Consume(reader, source, "font number", char.IsAsciiDigit);
                        int fontNumber = d0 - '0';
                        if (char.IsAsciiDigit((char)reader.Current()))
                        {
                            fontNumber = (fontNumber * 10) + (reader.Current() - '0');
                            reader.Advance();
                        }

                        if (fontNumber == 0)
                        {
                            throw new CompileErrorException(source, reader.Column, reader.Line,
                                "Font number cannot be zero");
                        }

                        RDLBlock(reader, false).FontNumber = (uint)fontNumber;
                        continue;
                    }
                    case 'h' when rdl:
                    {
                        // Read height tag
                        reader.Advance();
                        char h0 = Consume(reader, source, "height", char.IsAsciiDigit);
                        int height = h0 - '0';
                        if (char.IsAsciiDigit((char)reader.Current()))
                        {
                            height = (height * 10) + (reader.Current() - '0');
                            reader.Advance();
                        }
                        if (char.IsAsciiDigit((char)reader.Current()))
                        {
                            height = (height * 10) + (reader.Current() - '0');
                            reader.Advance();
                        }
                        if (height == 0)
                        {
                            throw new CompileErrorException(source, reader.Column, reader.Line,
                                "Height cannot be zero");
                        }
                        if (reader.Current() == '{')
                        {
                            throw new CompileErrorException(source, reader.Column, reader.Line,
                                "Height tags cannot be used for blocks");
                        }
                        ProcessRDL();
                        FlushWhitespace(); // maybe
                        if (rdlInText)
                        {
                            final.Append("</TEXT>");
                            rdlInText = false;
                        }
                        final.Append($"<POS h=\"{height}\" relH=\"true\"/>");
                        continue;
                    }
                    case 'l' when rdl:
                    case 'm' when rdl:
                    case 'r' when rdl:
                        curJust = (char)reader.Current();
                        reader.Advance();
                        if (reader.Current() == '{')
                        {
                            throw new CompileErrorException(source, reader.Column, reader.Line,
                                "Alignment tags cannot be used for blocks");
                        }
                        continue;
                    //Normal
                    default:
                        AppendChar((char)reader.Current());
                        break;
                }

                prev = reader.Current();
                reader.Advance();
            }
            else if (rdl && traStk.Count > 1 && cur == '}')
            {
                traStk.RemoveAt(traStk.Count - 1);
                prev = reader.Current();
                reader.Advance();
            }
            else if (rdl && cur == '&')
            {
                ProcessRDL();
                FlushWhitespace();
                AppendText("&amp;");
                prev = reader.Current();
                reader.Advance();
            }
            else if (rdl && cur == '<')
            {
                ProcessRDL();
                FlushWhitespace();
                AppendText("&lt;");
                prev = reader.Current();
                reader.Advance();
            }
            else if (rdl && cur == '>')
            {
                ProcessRDL();
                FlushWhitespace();
                AppendText("&gt;");
                prev = reader.Current();
                reader.Advance();
            }
            else if (cur == '\n')
            {
                whitespaceBuffer.Clear();
                isLineStart = true;
                deferredNewLine++;
                reader.Advance();
            }
            else if (IsSpace(cur))
            {
                if (!isLineStart)
                {
                    whitespaceBuffer.Append(cur);
                }

                prev = reader.Current();
                reader.Advance();
            }
            else
            {
                ProcessRDL();
                FlushWhitespace();
                AppendChar(cur);
                prev = reader.Current();
                reader.Advance();
            }
        }

        Finalize();
        return final.ToString();
    }

    static void UnexpectedError(FrcReader reader, string source, string expected)
    {
        string found = reader.Current() switch
        {
            -1 => "EOF",
            '\n' => "newline",
            _ => $"'{(char)reader.Current()}'"
        };
        throw new CompileErrorException(source, reader.Column, reader.Line,
            $"Unexpected {found}, expected {expected}.");
    }

    static void SkipComment(FrcReader reader)
    {
        reader.Advance();
        // Are we in a block comment?
        if (reader.Current() == '+')
        {
            char last = (char)reader.Current();
            reader.Advance();
            while (reader.Current() != -1 &&
                   !(last == ';' && reader.Current() == '-'))
            {
                last = (char)reader.Current();
                reader.Advance();
            }

            if (reader.Current() != -1)
                reader.Advance(); // Skip final `-`
        }
        else
        {
            SkipLine(reader);
        }
    }

    static void SkipLine(FrcReader reader)
    {
        int ch = reader.Current();
        while (ch != -1 && ch != '\n')
        {
            reader.Advance();
            ch = reader.Current();
        }
    }

    static bool SkipWhitespace(FrcReader reader)
    {
        int ch = reader.Current();
        while (ch != -1 && char.IsWhiteSpace((char)ch))
        {
            reader.Advance();
            ch = reader.Current();
        }

        return ch != -1;
    }
}
