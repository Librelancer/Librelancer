using System.IO;

namespace LibreLancer.ContentEdit.Frc;

public class FrcReader(string text, string source)
{
    public int Column { get; private set; } = 1;
    public int Line { get; private set; } = 1;

    private readonly StringReader reader = new(text);
    public string Source = source;

    //Returns -1 on EOF
    public int Current()
    {
        return reader.Peek();
    }

    public void Advance()
    {
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
