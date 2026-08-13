// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace LibreLancer;

public static class RandomExtensions
{
    public static float NextFloat(this Random rnd, float min, float max)
    {
        return (float)(
            min + (rnd.NextSingle() * (max - min))
        );
    }

    // 12-08-26: When inlined, seed stays in a register
    // While the results are on the stack.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint LCGXS24(ref uint seed)
    {
        var result = seed * 747796405U + 2891336453U;
        result ^= (result >> 14);
        seed = result;
        return (result >> 8);
    }

    [SkipLocalsInit]
    public static unsafe Vector3 NextUnitVector(this Random rnd)
    {
        var seed = (uint)rnd.Next();
        //We use a struct because stackalloc generates a guard cookie
        //Vector128.ConvertToSingle for uint generates more instructions than for int.
        //Use int since we only generate 24 random bits anyway (all we need for float).
        Vector4i v;
        v.X = (int)LCGXS24(ref seed);
        v.Y = (int)LCGXS24(ref seed);
        v.Z = (int)LCGXS24(ref seed);
        v.W = 0;
        var vec = Vector128.Load((int*)(&v));
        var s = Vector128.ConvertToSingle(vec);
        const float InvMaxInt = 1.0f / 16777216.0f;
        const float Multiplier = 2 * InvMaxInt;
        var res = Vector128.MultiplyAddEstimate(s, Vector128.Create<float>(Multiplier), Vector128.Create<float>(-1));
        return Vector3.Normalize(res.AsVector3());
    }
}
