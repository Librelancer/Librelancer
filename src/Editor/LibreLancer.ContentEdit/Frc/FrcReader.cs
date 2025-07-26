using System;
using System.IO;

namespace LibreLancer.ContentEdit.Frc;

class FrcReader(string text, string source)
{
    public int Column { get; private set; } = 1;
    public int Line { get; private set; } = 1;

    private int position = 0; //How many characters have we read ?

    private readonly StringReader reader = new(text);

    public string Source = source;

    private int readCount = 0; // For checking we haven't gotten into a loop
    public bool Match(string str)
    {
        var self = text.AsSpan(position);
        if (self.Length < str.Length)
            return false;
        return self.Slice(0, str.Length).SequenceEqual(str);
    }

    //Returns -1 on EOF
    public int Current()
    {
        readCount++;
        if (readCount > 10_000)
        {
            throw new InvalidOperationException("Infinite loop detected in FrcReader");
        }
        return reader.Peek();
    }

    public void Advance(int count)
    {
        for (int i = 0; i < count; i++)
            Advance();
    }

    public void Advance()
    {
        position++;
        readCount = 0;
        var n = reader.Read();
        if (n == '\n')
        {
            Line++;
            Column = 1;
        }
        else if (n != -1)
        {
            Column++;
        }
    }
}
