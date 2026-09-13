using System;

namespace LibreLancer.ContentEdit.RandomMissions;

class CompileErrorException : Exception
{
    public string Error { get; private set; }
    public int Column { get; private set; }
    public int Line { get; private set; }
    public string SourceFile { get; private set; }

    public CompileErrorException(Lexer lexer, string message)
        : this(lexer, lexer.Current, message)
    {
    }

    public CompileErrorException(Lexer lexer, Token token, string message) :
        this(lexer.Source, token.Column, token.Line, message)
    {
    }

    public CompileErrorException(string sourceFile, int column, int line, string message) :
        base($"{message} at {sourceFile}: {line}:{column}.")
    {
        Error = message;
        SourceFile = sourceFile;
        Line = line;
        Column = column;
    }
}
