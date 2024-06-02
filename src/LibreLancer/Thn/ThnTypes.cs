// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.Numerics;
using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
    public class ThnTypes
    {
        private static readonly Vector3[] axisTable =
        {
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ,
            -Vector3.UnitX,
            -Vector3.UnitY,
            -Vector3.UnitZ
        };
        public static ThnAxis ConvertAxis(object o, string source)
        {
            if (o is ThornTable tb)
            {

                var v3 = tb.ToVector3();
                var conv = axisTable.AsSpan().IndexOf(v3);
                if (conv == -1) conv = 0;
                FLLog.Error("Thn",
                    $"Incorrect axis format in {source}, '{tb}' should be {ThornTable.EnumReverse[((ThnAxis)conv).ToString()]}. Support for this will be removed in a later version");
                return (ThnAxis)conv;
            }

            return Convert<ThnAxis>(o);
        }
        //This method attempts to map any valid thorn value to a corresponding .NET object
        //It is somewhat fuzzy, and yes a huge mess.
        public static T Convert<T>(object o)
        {
            if (o is T o1) return o1;
            if (typeof(T) == typeof(Matrix4x4))
            {
                return (T)(object)GetMatrix((ThornTable) o);
            }
            if (typeof(T) == typeof(float))
            {
                if (o is int i) return (T)(object)(float)i;
                if(o is string s) return (T)(object)float.Parse(s, CultureInfo.InvariantCulture);
                throw new InvalidCastException(o.ToString() + " as float");
            }
            if (typeof(T) == typeof(int))
            {
                var fl = (int)Convert<float>(o);
                return (T)(object) fl;
            }
            if (o is string) return (T)ThnScript.ThnEnv[(string)o];
            if (typeof(T) == typeof(Vector3))
            {
                if (o is ThornTable tb) return (T)(object)tb.ToVector3();
            }
            if (typeof(T) == typeof(Color3f))
            {
                if (o is ThornTable tb) return (T) (object) new Color3f(tb.ToVector3());
            }
            if (typeof(T) == typeof(Quaternion))
            {
                if (o is ThornTable tb) return (T) (object) new Quaternion((float) tb[2], (float) tb[3], (float) tb[4], (float) tb[1]);
            }
            int integer = 0;
            if (o is float f) integer = (int) f;
            else if (o is int i) integer = i;
            else throw new Exception($"Unable to change type {o.GetType()} to {typeof(T)}");
            if (typeof(T) == typeof(bool)) return (T)(object)(integer != 0);
            return (T) System.Convert.ChangeType(integer, Enum.GetUnderlyingType(typeof(T)));
        }

        static Matrix4x4 GetMatrix(ThornTable orient)
        {
            var m11 = (float) ((ThornTable) orient[0])[0];
            var m12 = (float) ((ThornTable) orient[0])[1];
            var m13 = (float) ((ThornTable) orient[0])[2];

            var m21 = (float) ((ThornTable) orient[1])[0];
            var m22 = (float) ((ThornTable) orient[1])[1];
            var m23 = (float) ((ThornTable) orient[1])[2];

            var m31 = (float) ((ThornTable) orient[2])[0];
            var m32 = (float) ((ThornTable) orient[2])[1];
            var m33 = (float) ((ThornTable) orient[2])[2];
            return new Matrix4x4(
                m11, m12, m13, 0,
                m21, m22, m23, 0,
                m31, m32, m33, 0,
                0, 0, 0, 1
            );
        }

    }
}
