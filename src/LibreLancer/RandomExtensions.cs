using System;
using LibreLancer.Data;

namespace LibreLancer;

public static class RandomExtensions
{
    public static int Next(this Random random, ValueRange<int> range)
    {
        return random.Next(range.Min, range.Max + 1);
    }

    public static float Next(this Random random, ValueRange<float> range)
    {
        return range.Min + random.NextSingle() * (range.Max - range.Min);
    }
}