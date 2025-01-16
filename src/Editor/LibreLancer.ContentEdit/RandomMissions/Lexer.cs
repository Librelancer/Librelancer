using System;
using System.IO;
using System.Text;

namespace LibreLancer.ContentEdit.RandomMissions;

internal enum TokenKind
{
    Invalid,
    Identifier,
    Integer,
    Float,
    Semicolon,
    Comma,
    LeftParen,
    RightParen,
    EndOfFile
}

internal record struct Token(TokenKind Kind, string Value, int Line, int Column);

internal class Lexer
{
    private int column = 1;
    private Token current;

    private int line = 1;
    private StringReader reader;

    public string Source;

    public Lexer(string text, string source)
    {
        reader = new StringReader(text);
        Source = source;
    }

    public Token Current
    {
        get
        {
            if (current.Kind == TokenKind.Invalid)
                Next();
            return current;
        }
    }

    private int Peek()
    {
        return reader.Peek();
    }

    private void Read()
    {
        var n = reader.Read();
        if (n == '\n')
        {
            line++;
            column = 1;
        }
        else if (n != -1)
        {
            column++;
        }
    }

    private void SkipWhitespace()
    {
        int ch;
        while ((ch = Peek()) != -1 && char.IsWhiteSpace((char)ch))
            Read();
    }

    private static bool IsDigit(int c)
    {
        return c >= '0' && c <= '9';
    }

    private static bool IsLetter(int c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }

    private static bool IsIdentCharacter(int c)
    {
        return IsLetter(c) || IsDigit(c) || c == '_';
    }

    public bool IsIdentifier(string name, bool ignoreCase = false)
    {
        if (Current.Kind != TokenKind.Identifier)
            return false;
        return Current.Value.Equals(name, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
    }

    public void Next()
    {
        NextInternal();
        FLLog.Debug("Lexer", Current.ToString());
    }
    void NextInternal()
    {
        if (current.Kind == TokenKind.EndOfFile)
            throw new CompileErrorException(Source, column, line, "Unexpected end of file");


        SkipWhitespace();
        if (Peek() == -1)
        {
            current = new Token(TokenKind.EndOfFile, "", line, column);
            return;
        }

        while (Peek() == '#')
        {
            column++;
            Read();
            int ch;
            while ((ch = reader.Peek()) != -1 && ch != '\n')
                Read();
            SkipWhitespace();
        }

        if (Peek() == -1)
            current = new Token(TokenKind.EndOfFile, "end of file", line, column);

        var c = (char)Peek();
        if (c == ',')
        {
            current = new Token(TokenKind.Comma, ",", line, column);
            Read();
            return;
        }

        if (c == '(')
        {
            current = new Token(TokenKind.LeftParen, "(", line, column);
            Read();
            return;
        }

        if (c == ')')
        {
            current = new Token(TokenKind.RightParen, ")", line, column);
            Read();
            return;
        }

        if (c == ';')
        {
            current = new Token(TokenKind.Semicolon, ";", line, column);
            Read();
            return;
        }

        int tkLine = line, tkCol = column;
        if (IsDigit(c))
        {
            var sb = new StringBuilder();
            int ch;
            while ((ch = reader.Peek()) != -1 && IsDigit(ch))
            {
                sb.Append((char)ch);
                Read();
            }

            int dotLine = line, dotCol = column;
            if (ch == '.')
            {
                Read();
                if (!IsDigit(reader.Peek()))
                    throw new CompileErrorException(Source, column, line, "Unexpected '.'");
                sb.Append('.');
                while ((ch = reader.Peek()) != -1 && IsDigit(ch))
                {
                    sb.Append((char)ch);
                    Read();
                }

                current = new Token(TokenKind.Float, sb.ToString(), tkLine, tkCol);
                return;
            }

            current = new Token(TokenKind.Integer, sb.ToString(), tkLine, tkCol);
            return;
        }

        if (IsLetter(c) || c == '_')
        {
            var sb = new StringBuilder();
            int ch;
            while ((ch = reader.Peek()) != -1 && IsIdentCharacter(ch))
            {
                sb.Append((char)ch);
                Read();
            }

            current = new Token(TokenKind.Identifier, sb.ToString(), tkLine, tkCol);
            return;
        }

        throw new CompileErrorException(Source, tkLine, tkCol, $"Unexpected '{c}'");
    }
}
