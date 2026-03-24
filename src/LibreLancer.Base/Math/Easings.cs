// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LibreLancer
{
    public enum EasingTypes : byte
	{
        //Matches Alchemy types
        Step = 0,
		Linear = 1,
		EaseIn = 2,
		EaseOut = 3,
		EaseInOut = 4,
        EaseAuto = 5
    }
	public static class Easing
	{
        public static Color3f EaseColorRGB(EasingTypes type, float time, float t1, float t2, Color3f c1, Color3f c2)
		    => Unsafe.BitCast<Vector3, Color3f>(EaseColorRGB(type, time, t1, t2,
                    Unsafe.BitCast<Color3f,Vector3>(c1), Unsafe.BitCast<Color3f,Vector3>(c2)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 EaseColorRGB(EasingTypes type, float time, float t1, float t2, Vector3 v1, Vector3 v2)
        {
            var x = (time - t1) / (t2 - t1);
            float t;
            switch (type)
            {
                default:
                case EasingTypes.Step:
                    return v1;
                case EasingTypes.Linear:
                    t = x;
                    break;
                case EasingTypes.EaseIn:
                    t = x * x;
                    break;
                case EasingTypes.EaseOut:
                    t = 1.0f - (1.0f - x) * (1.0f - x);
                    break;
                case EasingTypes.EaseInOut:
                    t = x * x * (3.0f - 2 * x);
                    break;
                case EasingTypes.EaseAuto:
                    var eo = 1.0f - (1.0f - x) * (1.0f - x);
                    var ei = x * x;
                    var vt = Vector3.ConditionalSelect(
                        Vector3.GreaterThan(v1, v2), new(eo), new(ei)
                    );
                    return Vector3.Lerp(v1, v2, vt);
            }

            return Vector3.Lerp(v1, v2, t);
        }

		public static float Ease(EasingTypes type, float time, float t1, float t2, float v1, float v2)
        {
            var x = (time - t1) / (t2 - t1);
            switch (type)
            {
                default:
                case EasingTypes.Step:
                    return v1;
                case EasingTypes.Linear:
                    return MathHelper.Lerp(v1, v2, x);
                case EasingTypes.EaseIn:
                    return MathHelper.Lerp(v1, v2, x * x);
                case EasingTypes.EaseOut:
                    return MathHelper.Lerp(v1, v2, 1.0f - (1.0f - x) * (1.0f - x));
                case EasingTypes.EaseInOut:
                    return MathHelper.Lerp(v1, v2, x * x * (3.0f - 2 * x));
                case EasingTypes.EaseAuto:
                    return MathHelper.Lerp(v1, v2, v1 > v2 ?
                        1.0f - (1.0f - x) * (1.0f - x)
                        : x * x);
            }
        }
    }
}

