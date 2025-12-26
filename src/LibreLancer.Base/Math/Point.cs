// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer;

public struct Point
{
    public static readonly Point Zero = new Point(0, 0);

    public int X;
    public int Y;
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static bool operator ==(Point a, Point b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Point a, Point b) => a.X != b.X || a.Y != b.Y;

    public override int GetHashCode()
    {
        unchecked
        {
            return (X * 31) + Y;
        }
    }
    public override bool Equals(object? obj) => obj is Point point && point == this;
    public override string ToString() => $"({X}, {Y})";
}
