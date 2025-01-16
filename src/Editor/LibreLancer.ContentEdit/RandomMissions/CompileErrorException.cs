using System;

namespace LibreLancer.ContentEdit.RandomMissions;

class CompileErrorException : Exception
{
    public string Error { get; private set; }
    public int Column { get; private set; }
    public int Line { get; private set; }
    public string Source { get; private set; }

    public CompileErrorException(Lexer lexer, string message)
        : this(lexer, lexer.Current, message)
    {
    }

    public CompileErrorException(Lexer lexer, Token token, string message) :
        this(lexer.Source, token.Column, token.Line, message)
    {
    }

    public CompileErrorException(string source, int column, int line, string message) :
        base($"{message} at {source}: {line}:{column}.")
    {
        Error = message;
        Source = source;
        Line = line;
        Column = column;
    }
}
