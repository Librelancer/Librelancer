using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.ContentEdit.RandomMissions;
using LibreLancer.Dll;

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
    }

    private enum SpecialCharacterState
    {
        None,
        Color,
        FontNumber,
        Pos,
        Unicode
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum AvailableColour
    {
        r,
        Gray,
        Blue,
        Green,
        Aqua,
        Red,
        Fuchsia,
        Yellow,
        White
    }

    private int currentIdsNumber = -1;
    private string currentString = string.Empty;

    private bool skipWhitespace;
    private char lastSymbol = '\0';
    private string currentSpecialString = string.Empty;
    private bool lastCharWasNewLine;
    private State state = State.Start;
    private SpecialCharacterState specialCharacterState = SpecialCharacterState.None;

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
            _ => throw new CompileErrorException(source, reader.Column, reader.Line,
                $"Unexpected character '{ch}'. Expected S, H/I")
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
                        res.Strings[currentIdsNumber & 0xFFFF] = currentString;
                        break;
                    case State.Info:
                        res.Infocards[currentIdsNumber & 0xFFFF] = InfocardStart + currentString + InfocardEnd;
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

                currentString = string.Empty;
                currentIdsNumber = -1;
                lastSymbol = ch;
                lastCharWasNewLine = false;
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
                    currentString += state is State.String ? "\n" : "<PARA/>";
                }

                lastCharWasNewLine = false;
            }

            switch (lastSymbol)
            {
                case '\\' when ch is ' ':
                    if (currentString.Length is 0)
                    {
                        currentString += ch;
                    }

                    lastSymbol = '\0';
                    break;
                case '\\':
                    if (HandleSpecialCharacter())
                    {
                        if (ch is not '\\' and not '\n')
                        {
                            currentString += ch;
                        }

                        lastSymbol = ch;
                    }
                    break;
                default:
                    if (ch is not '\\' and not '\n')
                    {
                        currentString += ch;
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
                res.Strings[currentIdsNumber & 0xFFFF] = currentString;
                break;
            case State.Info:
                res.Infocards[currentIdsNumber & 0xFFFF] = InfocardStart + currentString + InfocardEnd;
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
                    case '\\':
                        currentString += '\\';
                        break;
                    case 'b':
                        currentString += """<TRA bold="true"/>""";
                        break;
                    case 'B':
                        currentString += """<TRA bold="false"/>""";
                        break;
                    case 'c':
                        specialCharacterState = SpecialCharacterState.Color;
                        return false;
                    case 'C':
                        currentString += """<TRA color="default"/>""";
                        break;
                    case 'f':
                        specialCharacterState = SpecialCharacterState.FontNumber;
                        return false;
                    case 'F':
                        currentString += """<TRA font="default"/>""";
                        break;
                    case 'h':
                        specialCharacterState = SpecialCharacterState.Pos;
                        return false;
                    case 'i':
                        currentString += """<TRA italic="true"/>""";
                        break;
                    case 'I':
                        currentString += """<TRA italic="false"/>""";
                        break;
                    case 'l':
                        currentString += """<JUST loc="l"/>""";
                        break;
                    case 'm':
                        currentString += """<JUST loc="c"/>""";
                        break;
                    case 'n':
                        currentString += state is State.Info ? "<PARA/>" : "\n";
                        break;
                    case 'r':
                        currentString += """<JUST loc="r"/>""";
                        break;
                    case 'u':
                        currentString += """<TRA underline="true"/>""";
                        break;
                    case 'U':
                        currentString += """<TRA underline="false"/>""";
                        break;
                    case 'x':
                        specialCharacterState = SpecialCharacterState.Unicode;
                        return false;
                    case '.':
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
                    currentSpecialString += ch;

                    if (Enum.TryParse<AvailableColour>(currentSpecialString, out var color))
                    {
                        if (color == AvailableColour.r)
                        {
                            color = AvailableColour.Red;
                        }

                        currentString += $"""<TRA color="{color.ToString().ToLowerInvariant()}"/>""";
                        break;
                    }
                    if (currentSpecialString.Length == 6 && currentSpecialString.All(char.IsAsciiHexDigit))
                    {
                        currentString += $"""<TRA color="#{currentSpecialString.ToUpper()}"/>""";
                        break;
                    }
                    if (currentSpecialString.Length > 7 || ch is ' ')
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Provided colour was not a valid colour string.\nProvided String: {currentSpecialString}");
                    }

                    return false;
                }
                case SpecialCharacterState.FontNumber:
                    if (ch is not ' ')
                    {
                        currentSpecialString += ch;
                        return false;
                    }

                    if (int.TryParse(currentSpecialString, out var fontNumber) && fontNumber is > 0 and < 100)
                    {
                        currentString += $"""<TRA font="{fontNumber}"/>""";
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
                        currentSpecialString += ch;
                        return false;
                    }

                    if (int.TryParse(currentSpecialString, out var pos) && pos is > 0 and < 1000)
                    {
                        currentString += $"""<POS h="{pos}" relH="true"/>""";
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
                        currentSpecialString += ch;
                        return false;
                    }

                    if (currentSpecialString.Length is not 4)
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Provided unicode string was not four characters.");
                    }

                    if (!currentSpecialString.All(char.IsAsciiHexDigit))
                    {
                        throw new CompileErrorException(source, reader.Column, reader.Line,
                            $"Provided unicode was not a valid hex string.");
                    }

                    var hex = Convert.ToInt32(currentSpecialString, 16);
                    currentString += char.ConvertFromUtf32(hex);
                    reprintCharacter = true;
                    break;
            }

            currentSpecialString = string.Empty;
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
                currentString += ch.ToString();
                lastSymbol = ch;
                return false;
            }

            if (currentString.Length is 0)
            {
                throw new CompileErrorException(source, reader.Column, reader.Line,
                    $"Unexpected character '{ch}'. Expected number or whitespace.");
            }

            currentIdsNumber = int.Parse(currentString);

            if(resourceIndex is not -1)
            {
                var min = resourceIndex * 65536;
                var max = min + 65535;
                if (currentIdsNumber < min || currentIdsNumber > max)
                {
                    throw new CompileErrorException(source, reader.Column, reader.Line, $"{currentIdsNumber} is out of range ({min}, {max})");
                }
            }

            currentString = "";

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
}
