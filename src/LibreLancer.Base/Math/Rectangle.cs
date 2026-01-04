// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Runtime.InteropServices;

namespace LibreLancer;

[StructLayout(LayoutKind.Sequential)]
public struct Rectangle
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public Rectangle(int x, int y, int w, int h)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }
    public bool Contains(int x, int y)
    {
        return (
            x >= X &&
            x <= (X + Width) &&
            y >= Y &&
            y <= (Y + Height)
        );
    }
    public bool Contains(Point pt)
    {
        return Contains (pt.X, pt.Y);
    }

    public bool Intersects(Rectangle other)
    {
        return (other.X < (X + Width) &&
                X < (other.X + other.Width) &&
                other.Y < (Y + Height) &&
                Y < (other.Y + other.Height));
    }

    public static bool Clip(Rectangle rect, Rectangle clip, out Rectangle result)
    {
        int clippedX = System.Math.Max(rect.X, clip.X);
        int clippedY = System.Math.Max(rect.Y, clip.Y);
        int clippedWidth = System.Math.Min(rect.X + rect.Width, clip.X + clip.Width) - clippedX;
        int clippedHeight = System.Math.Min(rect.Y + rect.Height, clip.Y + clip.Height) - clippedY;
        // If there's no intersection, return an empty rectangle
        if (clippedWidth <= 0 || clippedHeight <= 0)
        {
            result = default;
            return false;
        }
        result = new Rectangle(clippedX, clippedY, clippedWidth, clippedHeight);
        return true;
    }

    public override bool Equals(object? obj) => obj is Rectangle rectangle && rectangle == this;
    public static bool operator ==(Rectangle a, Rectangle b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
    public static bool operator !=(Rectangle a, Rectangle b) => a.X != b.X || a.Y != b.Y || a.Width != b.Width || a.Height != b.Height;

    public override int GetHashCode()
    {
        unchecked
        {
            int hc = X;
            hc = hc * 314159 + Y;
            hc = hc * 314159 + Width;
            hc = hc * 314159 + Height;
            return hc;
        }
    }

    public float AspectRatio => (float) Width / (float) Height;
}
