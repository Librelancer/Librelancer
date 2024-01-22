// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics.Text
{
    public struct OptionalColor
    {
        public bool Equals(OptionalColor other)
        {
            return Color.Equals(other.Color) && Enabled == other.Enabled;
        }

        public override bool Equals(object obj)
        {
            return obj is OptionalColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Color, Enabled);
        }

        public static bool operator ==(OptionalColor left, OptionalColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionalColor left, OptionalColor right)
        {
            return !left.Equals(right);
        }

        public Color4 Color;
        public bool Enabled;

        public OptionalColor(Color4 color)
        {
            Color = color;
            Enabled = true;
        }
    }
}
