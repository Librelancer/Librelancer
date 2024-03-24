using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thorn.Libraries;

static class ThornMath
{
    static float N(object a) => Convert.ToSingle(a);
    static float DG(object a) => MathHelper.DegreesToRadians(Convert.ToSingle(a));
    public static object abs(object[] args) => MathF.Abs(N(args[0]));
    public static object acos(object[] args) => MathHelper.RadiansToDegrees(MathF.Acos(N(args[0])));
    public static object asin(object[] args) => MathHelper.RadiansToDegrees(MathF.Asin(N(args[0])));
    public static object atan(object[] args) => MathHelper.RadiansToDegrees(MathF.Atan(N(args[0])));
    public static object atan2(object[] args) => MathHelper.RadiansToDegrees(MathF.Atan2(N(args[0]), N(args[1])));
    public static object ceil(object[] args) => MathF.Ceiling(N(args[0]));
    public static object floor(object[] args) => MathF.Floor(N(args[0]));

    public static object sin(object[] args) => MathF.Sin(DG(args[0]));
    public static object cos(object[] args) => MathF.Cos(DG(args[0]));
    public static object tan(object[] args) => MathF.Cos(DG(args[0]));
    public static object sqrt(object[] args) => MathF.Sqrt(N(args[0]));

    public static object log(object[] args) => MathF.Log(N(args[0]));

    public static object log10(object[] args) => MathF.Log10(N(args[0]));

    public static object mod(object[] args) => MathF.IEEERemainder(N(args[0]), N(args[1]));

    public static object pow(object[] args) => MathF.Pow(N(args[0]), N(args[1]));
    public static object exp(object[] args) => MathF.Exp(N(args[0]));

    public static object deg(object[] args) => MathHelper.RadiansToDegrees(N(args[0]));
    public static object rad(object[] args) => MathHelper.DegreesToRadians(N(args[0]));

    public static object min(object[] args)
    {
        var m = N(args[0]);
        for (int i = 1; i < args.Length; i++)
        {
            var x = N(args[i]);
            if (x < m)
                m = x;
        }
        return m;
    }

    public static object max(object[] args)
    {
        var m = N(args[0]);
        for (int i = 1; i < args.Length; i++)
        {
            var x = N(args[i]);
            if (x > m)
                m = x;
        }
        return m;
    }

    public static object ldexp(object[] args)
    {
        var d1 = N(args[0]);
        var d2 = N(args[1]);
        return d1 * MathF.Pow(2, d2);
    }

    public static object frexp(object[] args)
    {
        const int FLT_MANT_BITS = 23;
        const int FLT_EXP_MASK = 0x7f800000;
        const int FLT_SGN_MASK = -1 - 0x7fffffff;
        const int FLT_MANT_MASK = 0x007fffff;
        const int FLT_EXP_CLR_MASK = FLT_SGN_MASK | FLT_MANT_MASK;

        var number = N(args[0]);
        int exponent = 0;
        int bits = BitConverter.SingleToInt32Bits(number);
        int exp = (int)((bits & FLT_EXP_MASK) >> FLT_MANT_BITS);
        exponent = 0;

        if (exp == 0xff || number == 0F)
            number += number;
        else
        {
            // Not zero and finite.
            exponent = exp - 126;
            if (exp == 0)
            {
                // Subnormal, scale number so that it is in [1, 2).
                number *= BitConverter.Int32BitsToSingle(0x4c000000); // 2^25
                bits = BitConverter.SingleToInt32Bits(number);
                exp = (int)((bits & FLT_EXP_MASK) >> FLT_MANT_BITS);
                exponent = exp - 126 - 25;
            }
            // Set exponent to -1 so that number is in [0.5, 1).
            number = BitConverter.Int32BitsToSingle((bits & FLT_EXP_CLR_MASK) | 0x3f000000);
        }

        return new ThornTuple(number, (float)exponent);
    }

    static object random(object[] args, ThornRuntime runtime)
    {
        runtime.Random ??= new Random();
        if (args.Length == 0)
            return runtime.Random.NextSingle();
        if (args.Length == 1)
            return (float)runtime.Random.Next(1, (int)(N(args[0]) + 1));
        return (float)runtime.Random.Next((int)(N(args[0])), (int)(N(args[1]) + 1));
    }


    public static void SetBuiltins(Dictionary<string, object> Env, ThornRuntime runtime)
    {
        //Math lib
        Env["abs"] = (ThornRuntimeFunction)abs;
        Env["sin"] = (ThornRuntimeFunction)sin;
        Env["cos"] = (ThornRuntimeFunction)cos;
        Env["tan"] = (ThornRuntimeFunction)tan;
        Env["asin"] = (ThornRuntimeFunction)asin;
        Env["acos"] = (ThornRuntimeFunction)acos;
        Env["atan"] = (ThornRuntimeFunction)atan;
        Env["atan2"] = (ThornRuntimeFunction)atan2;
        Env["ceil"] = (ThornRuntimeFunction)ceil;
        Env["floor"] = (ThornRuntimeFunction)floor;
        Env["mod"] = (ThornRuntimeFunction)mod;
        Env["frexp"]  = (ThornRuntimeFunction)frexp;
        Env["ldexp"] = (ThornRuntimeFunction)ldexp;
        Env["sqrt"] = (ThornRuntimeFunction)sqrt;
        Env["min"] = (ThornRuntimeFunction)min;
        Env["max"] = (ThornRuntimeFunction)max;
        Env["log"] = (ThornRuntimeFunction)log;
        Env["log10"] = (ThornRuntimeFunction)log10;
        Env["exp"] = (ThornRuntimeFunction)exp;
        Env["deg"] = (ThornRuntimeFunction)deg;
        Env["rad"] = (ThornRuntimeFunction)rad;
        Env["pow"] = (ThornRuntimeFunction)pow; //?
        Env["PI"] = MathF.PI;
        //Random (stateful)
        Env["randomseed"] = (ThornRuntimeFunction)((args) =>
        {
            var seed = (int)N(args[0]);
            runtime.Random = new Random(seed);
            return null;
        });
        Env["random"] = (ThornRuntimeFunction)((args) => random(args, runtime));
    }

}
