using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using LibreLancer.ContentEdit.RandomMissions;
using LibreLancer.Dll;
using Microsoft.Extensions.Primitives;

namespace LibreLancer.ContentEdit.Frc;

public class FrcCompiler
{
    private enum State
    {
        Start,
        String,
        StringIds,
        Info,
        InfoIds,
        InfoRaw,
    }

    private enum SpecialCharacterState
    {
        None,
        Color,
        FontNumber,
        Pos,
        Unicode,
        Raw
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum AvailableColor
    {
        r,
        z,
        g,
        b,
        c,
        m,
        y,
        w,
        Gray,
        Blue,
        Green,
        Aqua,
        Red,
        Fuchsia,
        Yellow,
        White
    }

    private enum CommentState
    {
        None,
        SingleLine,
        MultiLine
    }

    private int currentIdsNumber = -1;
    private readonly StringBuilder currentString = new();
    private readonly StringBuilder currentSpecialString = new();

    private uint colorMask;
    private CommentState commentState = CommentState.None;
    private bool skipWhitespace;
    private char lastSymbol = '\0';
    private bool lastCharWasNewLine;
    private State state = State.Start;
    private SpecialCharacterState specialCharacterState = SpecialCharacterState.None;
    private bool inTextNode;

    private static bool IsSpace(char c) => c is ' ' or '\t';

    public const string InfocardStart = "<?xml version=\"1.0\" encoding=\"UTF-16\"?><RDL><PUSH/>";
    public const string InfocardEnd = "<POP/></RDL>";

    public ResourceDll Compile(string text, string source, int resourceIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentNullException(nameof(text));
        }

        ResourceDll res = new();

        var reader = new FrcReader(text, source);

        var ch = (char)reader.Current();
        state = ch switch
        {
            'S' => State.StringIds,
            'H' or 'I' => State.InfoIds,
            ';' => State.Start,
            _ => throw new CompileErrorException(source, reader.Column, reader.Line,
                $"Unexpected character '{ch}'. Expected S, H/I (or a comment ;)")
        };

        while (reader.Current() != -1)
        {
            if (!Advance(state is not State.String and not State.Info || lastCharWasNewLine || skipWhitespace))
            {
                break;
            }

            if (skipWhitespace)
            {
                skipWhitespace = false;
                lastSymbol = ' ';
            }

            ch = (char)reader.Current();
            if (ch == '\r')
            {
                continue;
            }

            if (commentState is not CommentState.None)
            {
                if (commentState is CommentState.SingleLine)
                {
                    commentState = ch switch
                    {
                        '\n' => CommentState.None,
                        '+' when lastSymbol is ';' => CommentState.MultiLine,
                        _ => commentState
                    };
                }
                else if (lastSymbol is ';' && ch is '-')
                {
                    commentState = CommentState.None;
                }

                lastSymbol = ch;
                continue;
            }

            if (ch is ';')
            {
                commentState = CommentState.SingleLine;

                // Remove any spaces that were before the comment
                var index = currentString.Length - 1;
                for (; index >= 0; index--)
                {
                    if (!char.IsWhiteSpace(currentString[index]))
                    {
                        break;
                    }
                }

                if (index < currentString.Length - 1)
                {
                    currentString.Length = index + 1;
                }

                continue;
            }

            if (reader.Column is 1 && char.IsAsciiLetterUpper(ch))
            {
                // Line ended without finishing a special character
                if (specialCharacterState != SpecialCharacterState.None)
                {
                    var oldCh = ch;
                    ch = '\0';
                    HandleSpecialCharacter();
                    ch = oldCh;
                }

                switch (state)
                {
                    // We are changing the state, lets finish what we were doing with the previous state
                    case State.String:
                        res.Strings[currentIdsNumber & 0xFFFF] = currentString.ToString();
                        break;
                    case State.Info:
                        res.Infocards[currentIdsNumber & 0xFFFF] = InfocardStart + currentString +
                                                                   (inTextNode ? "</TEXT>" : "") + InfocardEnd;
                        break;
                    case State.InfoRaw:
                        res.Infocards[currentIdsNumber & 0xFFFF] = currentString.ToString();
                        break;
                    case State.StringIds:
                    case State.InfoIds:
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Unexpected character '{ch}'. Was expecting a {(state is State.StringIds ? "string" : "infocard")}.");
                    }
                    case State.Start:
                    default:
                        break;
                }

                state = ch switch
                {
                    'S' => State.StringIds,
                    'H' or 'I' => State.InfoIds,
                    _ => throw new CompileErrorException(source, reader.Column, reader.Line,
                        $"Unexpected character '{ch}'. Expected S, H/I")
                };

                currentString.Clear();
                currentIdsNumber = -1;
                lastSymbol = ch;
                lastCharWasNewLine = false;
                inTextNode = false;
                continue;
            }

            if (state is State.StringIds or State.InfoIds)
            {
                if (!HandleIds())
                {
                    continue;
                }

                state = state is State.StringIds ? State.String : State.Info;
            }

            // If tilde is passed as the first character, don't supply RDL structure
            if (state is State.Info && ch is '~' && currentString.Length is 0)
            {
                state = State.InfoRaw;
                continue;
            }

            if (ch is '\n')
            {
                // If we end our string with a backslash, don't add a new line to the string
                if (lastSymbol is not '\\')
                {
                    lastCharWasNewLine = true;
                }
                else
                {
                    skipWhitespace = true;
                }

                continue;
            }

            if (lastCharWasNewLine)
            {
                if (currentString.Length is not 0)
                {
                    if (state is State.Info)
                    {
                        AppendXmlElement("<PARA/>");
                    }
                    else
                    {
                        currentString.Append('\n');
                    }
                }

                lastCharWasNewLine = false;
            }

            switch (lastSymbol)
            {
                case '\\' when ch is ' ' && specialCharacterState is SpecialCharacterState.None:
                    if (currentString.Length is 0)
                    {
                        AppendText(ch);
                    }

                    lastSymbol = '\0';
                    break;
                case '\\':
                    if (HandleSpecialCharacter())
                    {
                        if (ch is not '\\' and not '\n')
                        {
                            AppendText(ch);
                        }

                        lastSymbol = ch;
                    }

                    break;
                default:
                    if (ch is not '\\' and not '\n')
                    {
                        AppendText(ch);
                    }

                    lastSymbol = ch;
                    break;
            }
        }

        // File ended without finishing a special character
        if (specialCharacterState != SpecialCharacterState.None)
        {
            ch = ' ';
            HandleSpecialCharacter();
        }

        switch (state)
        {
            case State.String:
                res.Strings[currentIdsNumber & 0xFFFF] = currentString.ToString();
                break;
            case State.Info:
                res.Infocards[currentIdsNumber & 0xFFFF] =
                    InfocardStart + currentString + (inTextNode ? "</TEXT>" : "") + InfocardEnd;
                break;
            case State.InfoRaw:
                res.Infocards[currentIdsNumber & 0xFFFF] = currentString.ToString();
                break;
            case State.Start:
                break;
            default:
                throw new CompileErrorException(source, reader.Column, reader.Line,
                    $"State was invalid at file end");
        }

        return res;

        bool HandleSpecialCharacter()
        {
            if (specialCharacterState is SpecialCharacterState.None)
            {
                switch (ch)
                {
                    case '\\' when state is State.Info:
                        AppendText('\\');
                        break;
                    case '\\':
                        currentString.Append('\\');
                        break;
                    case 'b' when state is State.Info:
                        AppendXmlElement("""<TRA bold="true"/>""");
                        break;
                    case 'B' when state is State.Info:
                        AppendXmlElement("""<TRA bold="false"/>""");
                        break;
                    case 'c' when state is State.Info:
                        specialCharacterState = SpecialCharacterState.Color;
                        colorMask = 0xFFFFFF;
                        return false;
                    case 'C' when state is State.Info:
                        AppendXmlElement("""<TRA color="default"/>""");
                        break;
                    case 'f' when state is State.Info:
                        specialCharacterState = SpecialCharacterState.FontNumber;
                        return false;
                    case 'F' when state is State.Info:
                        AppendXmlElement("""<TRA font="default"/>""");
                        break;
                    case 'h' when state is State.Info:
                        specialCharacterState = SpecialCharacterState.Pos;
                        return false;
                    case 'i' when state is State.Info:
                        AppendXmlElement("""<TRA italic="true"/>""");
                        break;
                    case 'I' when state is State.Info:
                        AppendXmlElement("""<TRA italic="false"/>""");
                        break;
                    case 'l' when state is State.Info:
                        AppendXmlElement("""<JUST loc="l"/>""");
                        break;
                    case 'm' when state is State.Info:
                        AppendXmlElement("""<JUST loc="c"/>""");
                        break;
                    case 'n':
                        if (state is State.Info)
                        {
                            AppendXmlElement("<PARA/>");
                        }
                        else
                        {
                            AppendText('\n');
                        }
                        break;
                    case 'r' when state is State.Info:
                        AppendXmlElement("""<JUST loc="r"/>""");
                        break;
                    case 'u' when state is State.Info:
                        AppendXmlElement("""<TRA underline="true"/>""");
                        break;
                    case 'U' when state is State.Info:
                        AppendXmlElement("""<TRA underline="false"/>""");
                        break;
                    case 'x':
                        specialCharacterState = SpecialCharacterState.Unicode;
                        return false;
                    case '.':
                        break;
                    case '<':
                        specialCharacterState = SpecialCharacterState.Raw;
                        currentSpecialString.Append('<');
                        return false;
                    case '1':
                        AppendText('\u2081');
                        break;
                    case '2':
                        AppendText('\u2082');
                        break;
                    case '3':
                        AppendText('\u2083');
                        break;
                    case '0':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        AppendText((char)(ch - '0' + '\u2070'));
                        break;
                    default:
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Unexpected character '{ch}'. Expected special character.");
                    }
                }

                lastSymbol = ch;
                return false;
            }

            bool reprintCharacter = false;
            switch (specialCharacterState)
            {
                case SpecialCharacterState.Color:
                {
                    if (currentSpecialString.Length is 0 && ch is 'd' or 'h' or 'l')
                    {
                        colorMask = ch switch
                        {
                            'd' => 0x404040,
                            'h' => 0x808080,
                            'l' => 0xC0C0C0,
                            _ => 0
                        };
                        return false;
                    }

                    currentSpecialString.Append(ch);

                    if (Enum.TryParse<AvailableColor>(currentSpecialString.ToString(), out var color))
                    {
                        uint? singleCharColor = color switch
                        {
                            AvailableColor.z => 0x000000,
                            AvailableColor.r => 0xFF0000,
                            AvailableColor.g => 0x00FF00,
                            AvailableColor.b => 0x0000FF,
                            AvailableColor.c => 0x00FFFF,
                            AvailableColor.m => 0xFF00FF,
                            AvailableColor.y => 0xFFFF00,
                            AvailableColor.w => 0xFFFFFF,
                            _ => null
                        };

                        if (singleCharColor is not null)
                        {
                            var newCol = singleCharColor.Value & colorMask;
                            AppendXmlElement($"""<TRA color="#{newCol:X6}"/>""");
                            break;
                        }

                        AppendXmlElement($"""<TRA color="{color.ToString().ToLowerInvariant()}"/>""");
                        break;
                    }

                    if (currentSpecialString.Length == 6 && currentSpecialString.ToString().All(char.IsAsciiHexDigit))
                    {
                        AppendXmlElement($"""<TRA color="#{currentSpecialString.ToString().ToUpper()}"/>""");
                        break;
                    }

                    if (currentSpecialString.Length > 7 || ch is ' ')
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Provided color was not a valid color string.\nProvided String: {currentSpecialString}");
                    }

                    return false;
                }
                case SpecialCharacterState.FontNumber:
                    if (ch is not ' ')
                    {
                        currentSpecialString.Append(ch);
                        return false;
                    }

                    if (int.TryParse(currentSpecialString.ToString(), out var fontNumber) &&
                        fontNumber is > 0 and < 100)
                    {
                        AppendXmlElement($"""<TRA font="{fontNumber}"/>""");
                    }
                    else
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Provided font was not a valid number. Should be between 1-99 (inclusive).");
                    }

                    break;
                case SpecialCharacterState.Pos:
                    if (ch is not ' ')
                    {
                        currentSpecialString.Append(ch);
                        return false;
                    }

                    if (int.TryParse(currentSpecialString.ToString(), out var pos) && pos is > 0 and < 1000)
                    {
                        AppendXmlElement($"""<POS h="{pos}" relH="true"/>""");
                    }
                    else
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Provided pos was not a valid number. Should be between 1-999 (inclusive).");
                    }

                    break;
                case SpecialCharacterState.Unicode:
                    if (currentSpecialString.Length is not 4 && ch is not ' ')
                    {
                        currentSpecialString.Append(ch);
                        return false;
                    }

                    if (currentSpecialString.Length is not 4)
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Provided unicode string was not four characters.");
                    }

                    if (!currentSpecialString.ToString().All(char.IsAsciiHexDigit))
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Provided unicode was not a valid hex string.");
                    }

                    var hex = Convert.ToInt32(currentSpecialString.ToString(), 16);

                    if (state is State.Info)
                    {
                        foreach (var c in char.ConvertFromUtf32(hex))
                        {
                            AppendText(c);
                        }
                    }
                    else
                    {
                        currentString.Append(char.ConvertFromUtf32(hex));
                    }

                    reprintCharacter = true;
                    break;
                case SpecialCharacterState.Raw:
                    if (ch is not '>')
                    {
                        currentSpecialString.Append(ch);
                        return false;
                    }

                    currentSpecialString.Append(ch);
                    AppendXmlElement(currentSpecialString.ToString());
                    break;
            }

            currentSpecialString.Clear();
            specialCharacterState = SpecialCharacterState.None;
            lastSymbol = ch;
            return reprintCharacter;
        }

        bool HandleIds()
        {
            if (currentString.Length is 0 && IsSpace(ch))
            {
                return false;
            }

            // Set the IDS number and start reading text
            if (char.IsNumber(ch))
            {
                currentString.Append(ch.ToString());
                lastSymbol = ch;
                return false;
            }

            if (currentString.Length is 0)
            {
                throw new CompileErrorException(source, reader.Column, reader.Line,
                    $"Unexpected character '{ch}'. Expected number or whitespace.");
            }

            currentIdsNumber = int.Parse(currentString.ToString());

            if (resourceIndex is not -1)
            {
                var min = resourceIndex * 65536;
                var max = min + 65535;
                if (currentIdsNumber < min || currentIdsNumber > max)
                {
                    throw new CompileErrorException(source, reader.Column, reader.Line,
                        $"{currentIdsNumber} is out of range ({min}, {max})");
                }
            }

            currentString.Clear();

            return true;
        }

        bool Advance(bool skip)
        {
            reader.Advance();

            if (!skip)
            {
                return reader.Current() != -1;
            }

            int character;
            while ((character = reader.Current()) != -1 && IsSpace((char)character))
            {
                reader.Advance();
            }

            return (character != -1);

        }
    }

    private void AppendText(char text)
    {
        if (state is not State.Info)
        {
            currentString.Append(text);
            return;
        }

        if (!inTextNode)
        {
            inTextNode = true;
            currentString.Append("<TEXT>");
        }

        currentString.Append(EscapeInnerXml(text));
    }

    private void AppendXmlElement(string node)
    {
        if (inTextNode)
        {
            inTextNode = false;
            currentString.Append("</TEXT>");
        }

        currentString.Append(node);
    }

    private string EscapeInnerXml(char str)
    {
        if (state is not State.Info)
        {
            return str.ToString();
        }

        return str switch
        {
            '<' => "&lt;",
            '>' => "&gt;",
            '&' => "&amp;",
            _ => str.ToString()
        };
    }
}
