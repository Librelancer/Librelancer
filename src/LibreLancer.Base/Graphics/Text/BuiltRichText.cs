using System;
using BlurgText;

namespace LibreLancer.Graphics.Text;

public class BuiltRichText
{
    public required BlurgResult Result;
    public required BlurgFormattedText[]? Paragraphs;
    public required int[] NodeOffsets;
    public required Blurg? Parent;

    private int width = -1;

    public void Recalculate(float width)
    {
        if ((int) width == this.width)
            return;

        Result.Dispose();
        Result = Parent?.BuildFormattedText(Paragraphs, true, width) ?? throw new InvalidOperationException("Failed to build blurg text");
        this.width = (int) width;
    }

    public float Height => Result.Height;

    public Rectangle GetCaretPosition(int layoutIndex, int textPosition)
    {
        if (Result.Cursors.Length == 0 && Paragraphs is not null)
        {
            return new Rectangle(0, 0, 1, (int) Paragraphs[0].DefaultFont.LineHeight(Paragraphs[0].DefaultSize));
        }

        var pos = NodeOffsets[layoutIndex] + textPosition;

        if (pos == -1)
        {
            return new Rectangle(0, 0, 1, Result.Cursors[0].Height);
        }

        pos = MathHelper.Clamp(pos, 0, Result.Cursors.Length);
        var cur = Result.Cursors[pos];
        return new Rectangle(cur.X, cur.Y, 1, (int) cur.Height);
    }

    public void Dispose()
    {
        Result.Dispose();
        Parent = null;
        Paragraphs = null;
    }
}

