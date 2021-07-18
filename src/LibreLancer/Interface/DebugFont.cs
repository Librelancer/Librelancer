using System;
using System.Collections.Generic;

namespace LibreLancer
{
    static partial class DebugFont
    {
        public class DebugGlyph
        {
            public Rectangle Source;
            public int XOffset;
            public int YOffset;
            public int XAdvance;
            public DebugGlyph(int x, int y, int width, int height, int xoffset, int yoffset, int xadvance)
            {
                Source = new Rectangle(x, y, width, height);
                XOffset = xoffset;
                YOffset = yoffset;
                XAdvance = xadvance;
            }
        }
    }
}