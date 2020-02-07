// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer
{
    public struct TextShadow
    {
        public Color4 Color;
        public bool Enabled;

        public TextShadow(Color4 color)
        {
            Color = color;
            Enabled = true;
        }
    }
}